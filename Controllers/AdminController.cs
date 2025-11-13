using System;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using FileSharePortal.Data;
using FileSharePortal.Models;
using FileSharePortal.Services;

namespace FileSharePortal.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly FileSharePortalContext _context;
        private readonly AuthenticationService _authService;
        private readonly NotificationService _notificationService;
        private readonly RoleService _roleService;
        private readonly ADSyncService _adSyncService;

        public AdminController()
        {
            _context = new FileSharePortalContext();
            _authService = new AuthenticationService();
            _notificationService = new NotificationService();
            _roleService = new RoleService();
            _adSyncService = new ADSyncService();
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null || !currentUser.IsAdmin)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary
                    {
                        { "controller", "Account" },
                        { "action", "AccessDenied" }
                    });
            }
            base.OnActionExecuting(filterContext);
        }

        public ActionResult Index()
        {
            var currentUser = _authService.GetCurrentUser();
            ViewBag.CurrentUser = currentUser;

            ViewBag.TotalUsers = _context.Users.Count(u => u.IsActive);
            ViewBag.TotalFiles = _context.SharedFiles.Count(f => !f.IsDeleted);
            ViewBag.TotalRoles = _context.Roles.Count();
            ViewBag.PendingReports = _context.FileReports.Count(r => r.Status == ReportStatus.Pending);

            return View();
        }

        // User Management
        public ActionResult Users()
        {
            var currentUser = _authService.GetCurrentUser();
            ViewBag.CurrentUser = currentUser;

            var users = _context.Users.OrderBy(u => u.FullName).ToList();
            return View(users);
        }

        [HttpGet]
        public ActionResult CreateUser()
        {
            var currentUser = _authService.GetCurrentUser();
            ViewBag.CurrentUser = currentUser;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateUser(string username, string password, string fullName, string email, bool? isAdmin)
        {
            if (_context.Users.Any(u => u.Username == username))
            {
                ViewBag.Error = "Username already exists.";
                return View();
            }

            var user = new User
            {
                Username = username,
                PasswordHash = AuthenticationService.HashPassword(password),
                FullName = fullName,
                Email = email,
                IsAdmin = isAdmin.HasValue ? isAdmin.Value : false,
                IsActive = true,
                IsFromActiveDirectory = false
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            TempData["Success"] = "User created successfully!";
            return RedirectToAction("Users");
        }

        [HttpPost]
        public JsonResult ToggleUserStatus(int userId)
        {
            var user = _context.Users.Find(userId);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                _context.SaveChanges();
                return Json(new { success = true, isActive = user.IsActive });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public JsonResult ToggleAdminStatus(int userId)
        {
            var user = _context.Users.Find(userId);
            if (user != null)
            {
                user.IsAdmin = !user.IsAdmin;
                _context.SaveChanges();
                return Json(new { success = true, isAdmin = user.IsAdmin });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public JsonResult MassUpdateUserStatus(int[] userIds, bool isActive)
        {
            try
            {
                if (userIds == null || userIds.Length == 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "No users selected"
                    });
                }

                var users = _context.Users.Where(u => userIds.Contains(u.UserId)).ToList();

                if (users.Count == 0)
                {
                    return Json(new
                    {
                        success = false,
                        message = "No valid users found"
                    });
                }

                int updatedCount = 0;
                foreach (var user in users)
                {
                    if (user.IsActive != isActive)
                    {
                        user.IsActive = isActive;
                        updatedCount++;
                    }
                }

                _context.SaveChanges();

                var statusText = isActive ? "active" : "inactive";
                return Json(new
                {
                    success = true,
                    message = $"Successfully updated {updatedCount} user(s) to {statusText} status"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Error updating user status: {ex.Message}"
                });
            }
        }

        [HttpPost]
        public JsonResult SyncADUsers()
        {
            try
            {
                var result = _adSyncService.SynchronizeADUsers();

                if (result.Success)
                {
                    return Json(new
                    {
                        success = true,
                        message = result.GetSummary(),
                        totalADUsers = result.TotalADUsers,
                        usersAdded = result.UsersAdded,
                        usersUpdated = result.UsersUpdated,
                        usersReactivated = result.UsersReactivated,
                        usersDisabled = result.UsersDisabled
                    });
                }
                else
                {
                    return Json(new
                    {
                        success = false,
                        message = result.ErrorMessage
                    });
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Synchronization failed: {ex.Message}"
                });
            }
        }

        [HttpPost]
        public JsonResult ResetPassword(int userId, string newPassword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Password must be at least 6 characters long"
                    });
                }

                var user = _context.Users.Find(userId);

                if (user == null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "User not found"
                    });
                }

                // Prevent password reset for AD users
                if (user.IsFromActiveDirectory)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Cannot reset password for Active Directory users"
                    });
                }

                // Update password
                user.PasswordHash = AuthenticationService.HashPassword(newPassword);
                _context.SaveChanges();

                // Create notification for user
                _notificationService.CreateNotification(
                    user.UserId,
                    "Password Reset",
                    "Your password has been reset by an administrator. Please login with your new password.",
                    NotificationType.General
                );

                return Json(new
                {
                    success = true,
                    message = $"Password successfully reset for user '{user.Username}'"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = $"Error resetting password: {ex.Message}"
                });
            }
        }

        // Role Management
        public ActionResult Roles()
        {
            var currentUser = _authService.GetCurrentUser();
            ViewBag.CurrentUser = currentUser;

            var roles = _context.Roles.OrderBy(r => r.RoleName).ToList();
            return View(roles);
        }

        [HttpGet]
        public ActionResult CreateRole()
        {
            var currentUser = _authService.GetCurrentUser();
            ViewBag.CurrentUser = currentUser;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateRole(string roleName, string description)
        {
            var currentUser = _authService.GetCurrentUser();

            var role = new Role
            {
                RoleName = roleName,
                Description = description,
                CreatedByUserId = currentUser.UserId
            };

            _context.Roles.Add(role);
            _context.SaveChanges();

            TempData["Success"] = "Role created successfully!";
            return RedirectToAction("Roles");
        }

        public ActionResult ManageRole(int id)
        {
            var currentUser = _authService.GetCurrentUser();
            ViewBag.CurrentUser = currentUser;

            var role = _context.Roles.Find(id);
            if (role == null)
            {
                return HttpNotFound();
            }

            ViewBag.Role = role;
            ViewBag.AllUsers = _context.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToList();
            ViewBag.RoleUsers = _context.RoleUsers.Where(ru => ru.RoleId == id).Select(ru => ru.User).ToList();
            ViewBag.DistributionLists = _context.DistributionLists.ToList();
            ViewBag.RoleDistributionLists = _context.RoleDistributionLists
                .Where(rdl => rdl.RoleId == id)
                .Select(rdl => rdl.DistributionList)
                .ToList();

            return View();
        }

        [HttpPost]
        public JsonResult AddUserToRole(int roleId, int userId)
        {
            if (!_context.RoleUsers.Any(ru => ru.RoleId == roleId && ru.UserId == userId))
            {
                var roleUser = new RoleUser
                {
                    RoleId = roleId,
                    UserId = userId
                };
                _context.RoleUsers.Add(roleUser);
                _context.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "User already in role" });
        }

        [HttpPost]
        public JsonResult RemoveUserFromRole(int roleId, int userId)
        {
            var roleUser = _context.RoleUsers.FirstOrDefault(ru => ru.RoleId == roleId && ru.UserId == userId);
            if (roleUser != null)
            {
                _context.RoleUsers.Remove(roleUser);
                _context.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public JsonResult DeleteRole(int roleId)
        {
            try
            {
                var role = _context.Roles.Find(roleId);
                if (role == null)
                {
                    return Json(new { success = false, message = "Role not found" });
                }

                // Remove all users from this role
                var roleUsers = _context.RoleUsers.Where(ru => ru.RoleId == roleId).ToList();
                _context.RoleUsers.RemoveRange(roleUsers);

                // Remove all distribution lists from this role
                var roleDistLists = _context.RoleDistributionLists.Where(rdl => rdl.RoleId == roleId).ToList();
                _context.RoleDistributionLists.RemoveRange(roleDistLists);

                // Remove all file shares that use this role
                var fileShares = _context.FileShares.Where(fs => fs.SharedWithRoleId == roleId).ToList();
                _context.FileShares.RemoveRange(fileShares);

                // Delete the role
                _context.Roles.Remove(role);
                _context.SaveChanges();

                return Json(new { success = true, message = $"Role '{role.RoleName}' deleted successfully. {fileShares.Count} file shares removed." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error deleting role: {ex.Message}" });
            }
        }

        // Distribution Lists
        public ActionResult DistributionLists()
        {
            var currentUser = _authService.GetCurrentUser();
            ViewBag.CurrentUser = currentUser;

            var distributionLists = _context.DistributionLists.OrderBy(dl => dl.Name).ToList();
            return View(distributionLists);
        }

        [HttpGet]
        public ActionResult CreateDistributionList()
        {
            var currentUser = _authService.GetCurrentUser();
            ViewBag.CurrentUser = currentUser;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateDistributionList(string name, string adDistinguishedName, string description)
        {
            var distributionList = new DistributionList
            {
                Name = name,
                ADDistinguishedName = adDistinguishedName,
                Description = description
            };

            _context.DistributionLists.Add(distributionList);
            _context.SaveChanges();

            TempData["Success"] = "Distribution list created successfully!";
            return RedirectToAction("DistributionLists");
        }

        [HttpPost]
        public JsonResult AddDistributionListToRole(int roleId, int distributionListId)
        {
            if (!_context.RoleDistributionLists.Any(rdl => rdl.RoleId == roleId && rdl.DistributionListId == distributionListId))
            {
                var roleDistributionList = new RoleDistributionList
                {
                    RoleId = roleId,
                    DistributionListId = distributionListId
                };
                _context.RoleDistributionLists.Add(roleDistributionList);
                _context.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false, message = "Distribution list already in role" });
        }

        [HttpPost]
        public JsonResult RemoveDistributionListFromRole(int roleId, int distributionListId)
        {
            var roleDistributionList = _context.RoleDistributionLists
                .FirstOrDefault(rdl => rdl.RoleId == roleId && rdl.DistributionListId == distributionListId);
            if (roleDistributionList != null)
            {
                _context.RoleDistributionLists.Remove(roleDistributionList);
                _context.SaveChanges();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }

        // File Management
        public ActionResult Files()
        {
            var currentUser = _authService.GetCurrentUser();
            ViewBag.CurrentUser = currentUser;

            var files = _context.SharedFiles
                .Where(f => !f.IsDeleted)
                .OrderByDescending(f => f.UploadedDate)
                .ToList();

            return View(files);
        }

        [HttpPost]
        public JsonResult DeleteFile(int fileId)
        {
            var currentUser = _authService.GetCurrentUser();
            var file = _context.SharedFiles.Find(fileId);

            if (file != null)
            {
                file.IsDeleted = true;
                file.DeletedDate = DateTime.Now;
                file.DeletedByUserId = currentUser.UserId;
                _context.SaveChanges();

                // Notify file owner
                _notificationService.CreateNotification(
                    file.UploadedByUserId,
                    "File Deleted by Administrator",
                    $"Your file '{file.FileName}' has been deleted by an administrator.",
                    NotificationType.FileDeleted,
                    file.FileId
                );

                return Json(new { success = true });
            }

            return Json(new { success = false });
        }

        // File Reports
        public ActionResult Reports()
        {
            var currentUser = _authService.GetCurrentUser();
            ViewBag.CurrentUser = currentUser;

            var reports = _context.FileReports
                .OrderByDescending(r => r.ReportedDate)
                .ToList();

            return View(reports);
        }

        public ActionResult ReviewReport(int id)
        {
            var currentUser = _authService.GetCurrentUser();
            ViewBag.CurrentUser = currentUser;

            var report = _context.FileReports.Find(id);
            if (report == null)
            {
                return HttpNotFound();
            }

            return View(report);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ReviewReport(int reportId, string status, string adminNotes)
        {
            var currentUser = _authService.GetCurrentUser();
            var report = _context.FileReports.Find(reportId);

            if (report != null)
            {
                report.Status = (ReportStatus)Enum.Parse(typeof(ReportStatus), status);
                report.ReviewedByUserId = currentUser.UserId;
                report.ReviewedDate = DateTime.Now;
                report.AdminNotes = adminNotes;
                _context.SaveChanges();

                // Notify reporter
                _notificationService.CreateNotification(
                    report.ReportedByUserId,
                    "File Report Reviewed",
                    $"Your report for file '{report.SharedFile.FileName}' has been reviewed. Status: {report.Status}",
                    NotificationType.General,
                    report.FileId
                );

                TempData["Success"] = "Report reviewed successfully!";
            }

            return RedirectToAction("Reports");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context?.Dispose();
                _authService?.Dispose();
                _notificationService?.Dispose();
                _roleService?.Dispose();
                _adSyncService?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
