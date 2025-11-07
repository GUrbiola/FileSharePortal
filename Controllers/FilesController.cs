using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FileSharePortal.Data;
using FileSharePortal.Models;
using FileSharePortal.Services;

namespace FileSharePortal.Controllers
{
    public class FilesController : Controller
    {
        private readonly FileSharePortalContext _context;
        private readonly AuthenticationService _authService;
        private readonly NotificationService _notificationService;
        private readonly RoleService _roleService;

        public FilesController()
        {
            _context = new FileSharePortalContext();
            _authService = new AuthenticationService();
            _notificationService = new NotificationService();
            _roleService = new RoleService();
        }

        public ActionResult Index()
        {
            var currentUser = _authService.GetCurrentUser();
            ViewBag.CurrentUser = currentUser;

            var files = _context.SharedFiles
                .Where(f => f.UploadedByUserId == currentUser.UserId && !f.IsDeleted)
                .OrderByDescending(f => f.UploadedDate)
                .ToList();

            // Update FileSize if it's 0 but we have content
            foreach (var file in files)
            {
                if (file.FileSize == 0 && file.FileContent != null && file.FileContent.Length > 0)
                {
                    file.FileSize = file.FileContent.Length;
                }
            }

            return View(files);
        }

        public ActionResult SharedWithMe()
        {
            var currentUser = _authService.GetCurrentUser();
            ViewBag.CurrentUser = currentUser;

            var sharedFiles = new HashSet<SharedFile>();

            // Get files shared directly with the user
            var directShares = _context.FileShares
                .Where(fs => fs.SharedWithUserId == currentUser.UserId && !fs.SharedFile.IsDeleted)
                .Include(fs => fs.SharedFile.UploadedBy)
                .Select(fs => fs.SharedFile)
                .ToList();

            foreach (var file in directShares)
            {
                sharedFiles.Add(file);
            }

            // Get files shared through roles (including distribution list memberships)
            var roleShares = _context.FileShares
                .Where(fs => fs.SharedWithRoleId.HasValue && !fs.SharedFile.IsDeleted)
                .Include(fs => fs.SharedFile.UploadedBy)
                .ToList();

            foreach (var roleShare in roleShares)
            {
                var roleUsers = _roleService.GetRoleUsers(roleShare.SharedWithRoleId.Value);
                if (roleUsers.Any(u => u.UserId == currentUser.UserId))
                {
                    sharedFiles.Add(roleShare.SharedFile);
                }
            }

            var allFiles = sharedFiles.OrderByDescending(f => f.UploadedDate).ToList();

            // Update FileSize if it's 0 but we have content
            foreach (var file in allFiles)
            {
                if (file.FileSize == 0 && file.FileContent != null && file.FileContent.Length > 0)
                {
                    file.FileSize = file.FileContent.Length;
                }
            }

            return View(allFiles);
        }

        [HttpGet]
        public ActionResult Upload()
        {
            var currentUser = _authService.GetCurrentUser();
            ViewBag.CurrentUser = currentUser;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Upload(HttpPostedFileBase file, string description)
        {
            var currentUser = _authService.GetCurrentUser();

            if (file == null || file.ContentLength == 0)
            {
                ViewBag.Error = "Please select a file to upload.";
                return View();
            }

            try
            {
                var fileName = Path.GetFileName(file.FileName);

                // Read file content into byte array
                byte[] fileContent;
                using (var binaryReader = new BinaryReader(file.InputStream))
                {
                    fileContent = binaryReader.ReadBytes(file.ContentLength);
                }

                var sharedFile = new SharedFile
                {
                    FileName = fileName,
                    FilePath = null, // No longer storing on file system
                    FileContent = fileContent, // Store content in database
                    ContentType = file.ContentType,
                    FileSize = file.ContentLength,
                    UploadedByUserId = currentUser.UserId,
                    Description = description
                };

                _context.SharedFiles.Add(sharedFile);
                _context.SaveChanges();

                TempData["Success"] = "File uploaded successfully!";
                return RedirectToAction("Share", new { id = sharedFile.FileId });
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error uploading file: {ex.Message}";
                return View();
            }
        }

        [HttpGet]
        public ActionResult Share(int id)
        {
            var currentUser = _authService.GetCurrentUser();
            ViewBag.CurrentUser = currentUser;

            var file = _context.SharedFiles.Find(id);
            if (file == null || file.IsDeleted)
            {
                return HttpNotFound();
            }

            if (file.UploadedByUserId != currentUser.UserId && !currentUser.IsAdmin)
            {
                return new HttpUnauthorizedResult();
            }

            // Get existing shares
            var existingShares = _context.FileShares.Where(fs => fs.FileId == id).ToList();
            var sharedUserIds = existingShares.Where(fs => fs.SharedWithUserId.HasValue)
                                              .Select(fs => fs.SharedWithUserId.Value)
                                              .ToList();
            var sharedRoleIds = existingShares.Where(fs => fs.SharedWithRoleId.HasValue)
                                              .Select(fs => fs.SharedWithRoleId.Value)
                                              .ToList();

            if (file.FileSize == 0 && file.FileContent != null && file.FileContent.Length > 0)
            {
                file.FileSize = file.FileContent.Length;
            }

            ViewBag.File = file;
            ViewBag.Users = _context.Users.Where(u => u.IsActive && u.UserId != currentUser.UserId).ToList();
            ViewBag.Roles = _context.Roles.ToList();
            ViewBag.SharedUserIds = sharedUserIds;
            ViewBag.SharedRoleIds = sharedRoleIds;

            return View();
        }

        /// <summary>
        /// Calculates all users who have access to a file based on direct shares and role shares
        /// </summary>
        private HashSet<int> GetUsersWithAccess(int fileId)
        {
            var usersWithAccess = new HashSet<int>();

            // Get all shares for this file
            var fileShares = _context.FileShares.Where(fs => fs.FileId == fileId).ToList();

            foreach (var share in fileShares)
            {
                // Direct user share
                if (share.SharedWithUserId.HasValue)
                {
                    usersWithAccess.Add(share.SharedWithUserId.Value);
                }
                // Role share - get all users in that role
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Share(int fileId, int[] userIds, int[] roleIds)
        {
            var currentUser = _authService.GetCurrentUser();
            var file = _context.SharedFiles.Find(fileId);

            if (file == null || file.IsDeleted)
            {
                return HttpNotFound();
            }

            if (file.UploadedByUserId != currentUser.UserId && !currentUser.IsAdmin)
            {
                return new HttpUnauthorizedResult();
            }

            // Step 1: Get users with access BEFORE changes
            var usersBeforeUpdate = GetUsersWithAccess(fileId);

            // Step 2: Remove all existing shares (we'll recreate them)
            var existingShares = _context.FileShares.Where(fs => fs.FileId == fileId).ToList();
            _context.FileShares.RemoveRange(existingShares);
            _context.SaveChanges();

            // Step 3: Create new shares based on user selection
            // Share with users
            if (userIds != null && userIds.Length > 0)
            {
                foreach (var userId in userIds)
                {
                    var fileShare = new Models.FileShare
                    {
                        FileId = fileId,
                        SharedWithUserId = userId,
                        SharedByUserId = currentUser.UserId
                    };
                    _context.FileShares.Add(fileShare);
                }
            }

            // Share with roles
            if (roleIds != null && roleIds.Length > 0)
            {
                foreach (var roleId in roleIds)
                {
                    var fileShare = new Models.FileShare
                    {
                        FileId = fileId,
                        SharedWithRoleId = roleId,
                        SharedByUserId = currentUser.UserId
                    };
                    _context.FileShares.Add(fileShare);
                }
            }

            _context.SaveChanges();

            // Step 4: Get users with access AFTER changes
            var usersAfterUpdate = GetUsersWithAccess(fileId);

            // Step 5: Calculate differences
            var usersWhoGainedAccess = usersAfterUpdate.Except(usersBeforeUpdate).ToList();
            var usersWhoLostAccess = usersBeforeUpdate.Except(usersAfterUpdate).ToList();

            // Step 6: Send notifications to users who gained access
            if (usersWhoGainedAccess.Any())
            {
                _notificationService.NotifyFileShared(fileId, usersWhoGainedAccess, currentUser.UserId);
            }

            // Step 7: Send notifications to users who lost access
            if (usersWhoLostAccess.Any())
            {
                _notificationService.NotifyFileAccessRemoved(fileId, usersWhoLostAccess, currentUser.UserId);
            }

            TempData["Success"] = $"File shared successfully! {usersWhoGainedAccess.Count} users gained access, {usersWhoLostAccess.Count} users lost access.";
            return RedirectToAction("Details", new { id = fileId });
        }

        public ActionResult Details(int id)
        {
            var currentUser = _authService.GetCurrentUser();
            ViewBag.CurrentUser = currentUser;

            var file = _context.SharedFiles.Include(f => f.UploadedBy).FirstOrDefault(f => f.FileId == id);
            if (file == null || file.IsDeleted)
            {
                throw new HttpException(404, $"File was not found({id})!");
            }

            // Check if user has access (including through roles and distribution lists)
            if (!HasUserAccessToFile(id, currentUser.UserId))
            {
                throw new HttpException(403, $"Access denied({id})!");
            }

            ViewBag.SharedWith = _context.FileShares
                .Where(fs => fs.FileId == id)
                .Include(fs => fs.SharedWithUser)
                .Include(fs => fs.SharedWithRole)
                .ToList();

            // Get all users with access (for detailed view)
            var usersWithAccess = GetUsersWithAccess(id);

            // Add file owner to the list
            usersWithAccess.Add(file.UploadedByUserId);

            var usersWithAccessDetails = _context.Users
                .Where(u => usersWithAccess.Contains(u.UserId))
                .Select(u => new { u.UserId, u.FullName, u.Email })
                .ToList();
            ViewBag.UsersWithAccess = usersWithAccessDetails;

            // Update FileSize if it's 0 but we have content
            if (file.FileSize == 0 && file.FileContent != null && file.FileContent.Length > 0)
            {
                file.FileSize = file.FileContent.Length;
            }

            // Check if file is previewable
            var extension = System.IO.Path.GetExtension(file.FileName)?.ToLower();
            var previewableExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".pdf", ".txt", ".html", ".htm", ".xml", ".json", ".csv" };
            ViewBag.IsPreviewable = previewableExtensions.Contains(extension);
            ViewBag.IsImage = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp" }.Contains(extension);
            ViewBag.IsPdf = extension == ".pdf";
            ViewBag.IsText = new[] { ".txt", ".html", ".htm", ".xml", ".json", ".csv" }.Contains(extension);

            return View(file);
        }

        public ActionResult Preview(int id)
        {
            var currentUser = _authService.GetCurrentUser();
            var file = _context.SharedFiles.Find(id);

            if (file == null || file.IsDeleted)
            {
                throw new HttpException(404, $"File was not found({id})!");
            }

            // Check if user has access (including through roles and distribution lists)
            if (!HasUserAccessToFile(id, currentUser.UserId))
            {
                throw new HttpException(403, $"Access denied({id})!");
            }

            // Check if file content exists in database
            if (file.FileContent == null || file.FileContent.Length == 0)
            {
                return HttpNotFound("File content not found.");
            }

            return File(file.FileContent, file.ContentType);
        }

        public ActionResult Download(int id)
        {
            var currentUser = _authService.GetCurrentUser();
            var file = _context.SharedFiles.Find(id);

            if (file == null || file.IsDeleted)
            {
                throw new HttpException(404, $"File was not found({id})!");
            }

            // Check if user has access (including through roles and distribution lists)
            if (!HasUserAccessToFile(id, currentUser.UserId))
            {
                throw new HttpException(403, $"Access denied({id})!");
            }

            // Check if file content exists in database
            if (file.FileContent == null || file.FileContent.Length == 0)
            {
                return HttpNotFound("File content not found.");
            }

            // Increment download count
            file.DownloadCount++;
            _context.SaveChanges();

            return File(file.FileContent, file.ContentType, file.FileName);
        }

        [HttpGet]
        public ActionResult Report(int id)
        {
            var currentUser = _authService.GetCurrentUser();
            ViewBag.CurrentUser = currentUser;

            var file = _context.SharedFiles.Find(id);
            if (file == null || file.IsDeleted)
            {
                return HttpNotFound();
            }

            ViewBag.File = file;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Report(int fileId, string reason, string details)
        {
            var currentUser = _authService.GetCurrentUser();

            var report = new FileReport
            {
                FileId = fileId,
                ReportedByUserId = currentUser.UserId,
                Reason = reason,
                Details = details
            };

            _context.FileReports.Add(report);
            _context.SaveChanges();

            _notificationService.NotifyFileReported(report.ReportId);

            TempData["Success"] = "File reported successfully. Administrators have been notified.";
            return RedirectToAction("Details", new { id = fileId });
        }

        [HttpPost]
        public JsonResult Delete(int id)
        {
            try
            {
                var currentUser = _authService.GetCurrentUser();
                var file = _context.SharedFiles.Find(id);

                if (file == null || file.IsDeleted)
                {
                    return Json(new { success = false, message = "File not found" });
                }

                // Only file owner or admin can delete
                if (file.UploadedByUserId != currentUser.UserId && !currentUser.IsAdmin)
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                // Mark file as deleted (soft delete)
                file.IsDeleted = true;
                file.DeletedDate = DateTime.Now;
                file.DeletedByUserId = currentUser.UserId;

                // Clear file content from database to free up space
                file.FileContent = null;

                _context.SaveChanges();

                // Get all users who had access before deletion
                var usersWithAccess = GetUsersWithAccess(id);

                // Notify users who had access that the file was deleted
                if (usersWithAccess.Any())
                {
                    _notificationService.NotifyFileDeleted(id, usersWithAccess.ToList(), currentUser.UserId);
                }

                return Json(new { success = true, message = $"File '{file.FileName}' deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error deleting file: {ex.Message}" });
            }
        }

        /// <summary>
        /// Search users for autocomplete in share dialog
        /// </summary>
        public JsonResult SearchUsers(string term, int fileId)
        {
            var currentUser = _authService.GetCurrentUser();

            // Get file to check permissions
            var file = _context.SharedFiles.Find(fileId);
            if (file == null || file.IsDeleted)
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }

            // Only file owner or admin can search for sharing
            if (file.UploadedByUserId != currentUser.UserId && !currentUser.IsAdmin)
            {
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);
            }

            // Search for users (exclude current user)
            var searchTerm = (term ?? "").ToLower();
            var users = _context.Users
                .Where(u => u.IsActive &&
                            u.UserId != currentUser.UserId &&
                            (u.FullName.ToLower().Contains(searchTerm) ||
                             u.Username.ToLower().Contains(searchTerm) ||
                             u.Email.ToLower().Contains(searchTerm)))
                .OrderBy(u => u.FullName)
                .Take(20)
                .Select(u => new
                {
                    id = u.UserId,
                    text = u.FullName + " (" + u.Email + ")"
                })
                .ToList();

            return Json(users, JsonRequestBehavior.AllowGet);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context?.Dispose();
                _authService?.Dispose();
                _notificationService?.Dispose();
                _roleService?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
