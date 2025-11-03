using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using FileSharePortal.Data;
using FileSharePortal.Filters;
using FileSharePortal.Models;
using FileSharePortal.Services;

namespace FileSharePortal.Controllers.Api
{
    [RoutePrefix("api/files")]
    [ApiAuthentication]
    public class FilesApiController : BaseApiController
    {
        private readonly FileSharePortalContext _context;
        private readonly NotificationService _notificationService;
        private readonly RoleService _roleService;

        public FilesApiController()
        {
            _context = new FileSharePortalContext();
            _notificationService = new NotificationService();
            _roleService = new RoleService();
        }

        /// <summary>
        /// Get list of user's uploaded files
        /// GET /api/files
        /// </summary>
        [HttpGet]
        [Route("")]
        public IHttpActionResult GetFiles()
        {
            var files = _context.SharedFiles
                .Where(f => f.UploadedByUserId == CurrentUser.UserId && !f.IsDeleted)
                .OrderByDescending(f => f.UploadedDate)
                .Select(f => new
                {
                    fileId = f.FileId,
                    fileName = f.FileName,
                    fileSize = f.FileSize,
                    contentType = f.ContentType,
                    uploadedDate = f.UploadedDate,
                    downloadCount = f.DownloadCount,
                    description = f.Description
                })
                .ToList();

            return Ok(files);
        }

        /// <summary>
        /// Get files shared with the user
        /// GET /api/files/shared
        /// </summary>
        [HttpGet]
        [Route("shared")]
        public IHttpActionResult GetSharedFiles()
        {
            var files = _context.FileShares
                .Where(fs => fs.SharedWithUserId == CurrentUser.UserId && !fs.SharedFile.IsDeleted)
                .Select(fs => fs.SharedFile)
                .Distinct()
                .OrderByDescending(f => f.UploadedDate)
                .Select(f => new
                {
                    fileId = f.FileId,
                    fileName = f.FileName,
                    fileSize = f.FileSize,
                    contentType = f.ContentType,
                    uploadedDate = f.UploadedDate,
                    uploadedBy = f.UploadedBy.FullName,
                    downloadCount = f.DownloadCount,
                    description = f.Description
                })
                .ToList();

            return Ok(files);
        }

        /// <summary>
        /// Upload a file
        /// POST /api/files/upload
        /// Content-Type: multipart/form-data
        /// </summary>
        [HttpPost]
        [Route("upload")]
        public async Task<IHttpActionResult> UploadFile()
        {
            if (!Request.Content.IsMimeMultipartContent())
            {
                return BadRequest("Unsupported media type. Use multipart/form-data");
            }

            try
            {
                var uploadPath = HttpContext.Current.Server.MapPath("~/App_Data/Uploads");
                if (!Directory.Exists(uploadPath))
                {
                    Directory.CreateDirectory(uploadPath);
                }

                var provider = new MultipartFormDataStreamProvider(uploadPath);
                await Request.Content.ReadAsMultipartAsync(provider);

                if (provider.FileData.Count == 0)
                {
                    return BadRequest("No file uploaded");
                }

                var fileData = provider.FileData[0];
                var fileName = fileData.Headers.ContentDisposition.FileName.Trim('"');
                var contentType = fileData.Headers.ContentType.MediaType;
                var tempFilePath = fileData.LocalFileName;
                var fileInfo = new FileInfo(tempFilePath);

                // Generate unique file name
                var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
                var finalPath = Path.Combine(uploadPath, uniqueFileName);

                // Move temp file to final location
                File.Move(tempFilePath, finalPath);

                // Get description from form data if provided
                var description = provider.FormData["description"];

                var sharedFile = new SharedFile
                {
                    FileName = fileName,
                    FilePath = uniqueFileName,
                    ContentType = contentType,
                    FileSize = fileInfo.Length,
                    UploadedByUserId = CurrentUser.UserId,
                    Description = description
                };

                _context.SharedFiles.Add(sharedFile);
                _context.SaveChanges();

                return Ok(new
                {
                    fileId = sharedFile.FileId,
                    fileName = sharedFile.FileName,
                    fileSize = sharedFile.FileSize,
                    uploadedDate = sharedFile.UploadedDate,
                    message = "File uploaded successfully"
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Download a file
        /// GET /api/files/{id}/download
        /// </summary>
        [HttpGet]
        [Route("{id}/download")]
        public HttpResponseMessage Download(int id)
        {
            var file = _context.SharedFiles.Find(id);

            if (file == null || file.IsDeleted)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, new { error = "File not found" });
            }

            // Check if user has access (including through roles and distribution lists)
            if (!HasUserAccessToFile(id, CurrentUser.UserId, requireDownloadPermission: true))
            {
                return Request.CreateResponse(HttpStatusCode.Forbidden, new { error = "Access denied" });
            }

            var uploadPath = HttpContext.Current.Server.MapPath("~/App_Data/Uploads");
            var filePath = Path.Combine(uploadPath, file.FilePath);

            if (!File.Exists(filePath))
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, new { error = "File not found on disk" });
            }

            // Increment download count
            file.DownloadCount++;
            _context.SaveChanges();

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            response.Content = new StreamContent(stream);
            response.Content.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
            response.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = file.FileName
            };

            return response;
        }

        /// <summary>
        /// Delete a file
        /// DELETE /api/files/{id}
        /// </summary>
        [HttpDelete]
        [Route("{id}")]
        public IHttpActionResult DeleteFile(int id)
        {
            var file = _context.SharedFiles.Find(id);

            if (file == null || file.IsDeleted)
            {
                return NotFound();
            }

            // Only owner or admin can delete
            if (file.UploadedByUserId != CurrentUser.UserId && !CurrentUser.IsAdmin)
            {
                return Content(HttpStatusCode.Forbidden, new { error = "Access denied" });
            }

            file.IsDeleted = true;
            file.DeletedDate = DateTime.Now;
            _context.SaveChanges();

            return Ok(new { message = "File deleted successfully" });
        }

        /// <summary>
        /// Share a file with users and/or roles
        /// POST /api/files/{id}/share
        /// Body: { "userIds": [1, 2, 3], "roleIds": [1, 2] }
        /// </summary>
        [HttpPost]
        [Route("{id}/share")]
        public IHttpActionResult ShareFile(int id, [FromBody] ShareFileRequest request)
        {
            var file = _context.SharedFiles.Find(id);

            if (file == null || file.IsDeleted)
            {
                return NotFound();
            }

            // Only owner or admin can share
            if (file.UploadedByUserId != CurrentUser.UserId && !CurrentUser.IsAdmin)
            {
                return Content(HttpStatusCode.Forbidden, new { error = "Access denied" });
            }

            // Get users with access before changes
            var usersBeforeUpdate = GetUsersWithAccess(id);

            // Remove existing shares
            var existingShares = _context.FileShares.Where(fs => fs.FileId == id).ToList();
            _context.FileShares.RemoveRange(existingShares);
            _context.SaveChanges();

            // Create new shares
            if (request.UserIds != null && request.UserIds.Length > 0)
            {
                foreach (var userId in request.UserIds)
                {
                    var fileShare = new Models.FileShare
                    {
                        FileId = id,
                        SharedWithUserId = userId,
                        SharedByUserId = CurrentUser.UserId
                    };
                    _context.FileShares.Add(fileShare);
                }
            }

            if (request.RoleIds != null && request.RoleIds.Length > 0)
            {
                foreach (var roleId in request.RoleIds)
                {
                    var fileShare = new Models.FileShare
                    {
                        FileId = id,
                        SharedWithRoleId = roleId,
                        SharedByUserId = CurrentUser.UserId
                    };
                    _context.FileShares.Add(fileShare);
                }
            }

            _context.SaveChanges();

            // Get users with access after changes
            var usersAfterUpdate = GetUsersWithAccess(id);

            // Calculate differences and send notifications
            var usersWhoGainedAccess = usersAfterUpdate.Except(usersBeforeUpdate).ToList();
            var usersWhoLostAccess = usersBeforeUpdate.Except(usersAfterUpdate).ToList();

            if (usersWhoGainedAccess.Any())
            {
                _notificationService.NotifyFileShared(id, usersWhoGainedAccess, CurrentUser.UserId);
            }

            if (usersWhoLostAccess.Any())
            {
                _notificationService.NotifyFileAccessRemoved(id, usersWhoLostAccess, CurrentUser.UserId);
            }

            return Ok(new
            {
                message = "File shared successfully",
                usersGainedAccess = usersWhoGainedAccess.Count,
                usersLostAccess = usersWhoLostAccess.Count
            });
        }

        /// <summary>
        /// Remove access to a file for specific users/roles
        /// POST /api/files/{id}/remove-access
        /// Body: { "userIds": [1, 2], "roleIds": [1] }
        /// </summary>
        [HttpPost]
        [Route("{id}/remove-access")]
        public IHttpActionResult RemoveAccess(int id, [FromBody] ShareFileRequest request)
        {
            var file = _context.SharedFiles.Find(id);

            if (file == null || file.IsDeleted)
            {
                return NotFound();
            }

            // Only owner or admin can modify access
            if (file.UploadedByUserId != CurrentUser.UserId && !CurrentUser.IsAdmin)
            {
                return Content(HttpStatusCode.Forbidden, new { error = "Access denied" });
            }

            var usersAffected = new List<int>();

            // Remove user shares
            if (request.UserIds != null && request.UserIds.Length > 0)
            {
                var userShares = _context.FileShares
                    .Where(fs => fs.FileId == id && request.UserIds.Contains(fs.SharedWithUserId.Value))
                    .ToList();

                usersAffected.AddRange(request.UserIds);
                _context.FileShares.RemoveRange(userShares);
            }

            // Remove role shares
            if (request.RoleIds != null && request.RoleIds.Length > 0)
            {
                var roleShares = _context.FileShares
                    .Where(fs => fs.FileId == id && request.RoleIds.Contains(fs.SharedWithRoleId.Value))
                    .ToList();

                // Get users who will lose access through roles
                foreach (var roleId in request.RoleIds)
                {
                    var roleUsers = _roleService.GetRoleUsers(roleId);
                    usersAffected.AddRange(roleUsers.Select(u => u.UserId));
                }

                _context.FileShares.RemoveRange(roleShares);
            }

            _context.SaveChanges();

            // Send notifications
            if (usersAffected.Any())
            {
                _notificationService.NotifyFileAccessRemoved(id, usersAffected.Distinct().ToList(), CurrentUser.UserId);
            }

            return Ok(new
            {
                message = "Access removed successfully",
                usersAffected = usersAffected.Distinct().Count()
            });
        }

        private HashSet<int> GetUsersWithAccess(int fileId)
        {
            var usersWithAccess = new HashSet<int>();
            var fileShares = _context.FileShares.Where(fs => fs.FileId == fileId).ToList();

            foreach (var share in fileShares)
            {
                if (share.SharedWithUserId.HasValue)
                {
                    usersWithAccess.Add(share.SharedWithUserId.Value);
                }
                else if (share.SharedWithRoleId.HasValue)
                {
                    var roleUsers = _roleService.GetRoleUsers(share.SharedWithRoleId.Value);
                    foreach (var user in roleUsers)
                    {
                        usersWithAccess.Add(user.UserId);
                    }
                }
            }

            return usersWithAccess;
        }

        /// <summary>
        /// Check if a user has access to a file (including through roles and distribution lists)
        /// </summary>
        private bool HasUserAccessToFile(int fileId, int userId, bool requireDownloadPermission = false)
        {
            var file = _context.SharedFiles.Find(fileId);
            if (file == null)
                return false;

            // Owner and admin always have access
            var user = _context.Users.Find(userId);
            if (file.UploadedByUserId == userId || (user != null && user.IsAdmin))
                return true;

            // Check direct user shares
            var directShare = _context.FileShares.FirstOrDefault(fs =>
                fs.FileId == fileId &&
                fs.SharedWithUserId == userId);

            if (directShare != null)
            {
                if (requireDownloadPermission)
                    return directShare.CanDownload;
                return true;
            }

            // Check role shares (including distribution list members)
            var roleShares = _context.FileShares
                .Where(fs => fs.FileId == fileId && fs.SharedWithRoleId.HasValue)
                .ToList();

            foreach (var roleShare in roleShares)
            {
                var roleUsers = _roleService.GetRoleUsers(roleShare.SharedWithRoleId.Value);
                if (roleUsers.Any(u => u.UserId == userId))
                {
                    if (requireDownloadPermission)
                        return roleShare.CanDownload;
                    return true;
                }
            }

            return false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context?.Dispose();
                _notificationService?.Dispose();
                _roleService?.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class ShareFileRequest
    {
        public int[] UserIds { get; set; }
        public int[] RoleIds { get; set; }
    }
}
