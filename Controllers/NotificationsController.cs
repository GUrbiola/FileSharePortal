using System.Linq;
using System.Web.Mvc;
using FileSharePortal.Data;
using FileSharePortal.Services;

namespace FileSharePortal.Controllers
{
    public class NotificationsController : Controller
    {
        private readonly NotificationService _notificationService;
        private readonly AuthenticationService _authService;
        private readonly FileSharePortalContext _context;

        public NotificationsController()
        {
            _notificationService = new NotificationService();
            _authService = new AuthenticationService();
            _context = new FileSharePortalContext();
        }

        public ActionResult Index()
        {
            var currentUser = _authService.GetCurrentUser();
            ViewBag.CurrentUser = currentUser;

            var notifications = _notificationService.GetUserNotifications(currentUser.UserId);
            return View(notifications);
        }

        public ActionResult Details(int id)
        {
            var currentUser = _authService.GetCurrentUser();
            ViewBag.CurrentUser = currentUser;

            var notification = _context.Notifications.Find(id);
            if (notification == null)
            {
                return HttpNotFound();
            }

            // Check if notification belongs to current user
            if (notification.UserId != currentUser.UserId)
            {
                return new HttpUnauthorizedResult();
            }

            // Mark as read when viewing details
            if (!notification.IsRead)
            {
                _notificationService.MarkAsRead(id);
            }

            // Load related data
            if (notification.RelatedFileId.HasValue)
            {
                ViewBag.RelatedFile = _context.SharedFiles.Find(notification.RelatedFileId.Value);
            }

            //if (notification.RelatedUserId.HasValue)
            //{
            //    ViewBag.RelatedUser = _context.Users.Find(notification.RelatedUserId.Value);
            //}

            return View(notification);
        }

        [HttpPost]
        public JsonResult MarkAsRead(int id)
        {
            _notificationService.MarkAsRead(id);
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult MarkAllAsRead()
        {
            var currentUser = _authService.GetCurrentUser();
            _notificationService.MarkAllAsRead(currentUser.UserId);
            return Json(new { success = true });
        }

        [HttpPost]
        public JsonResult Delete(int id)
        {
            try
            {
                var currentUser = _authService.GetCurrentUser();
                var notification = _context.Notifications.Find(id);

                if (notification == null)
                {
                    return Json(new { success = false, message = "Notification not found" });
                }

                // Verify the notification belongs to the current user
                if (notification.UserId != currentUser.UserId)
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                _context.Notifications.Remove(notification);
                _context.SaveChanges();

                return Json(new { success = true, message = "Notification deleted successfully" });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = $"Error deleting notification: {ex.Message}" });
            }
        }

        [HttpPost]
        public JsonResult MassDelete(int[] notificationIds)
        {
            try
            {
                var currentUser = _authService.GetCurrentUser();

                if (notificationIds == null || notificationIds.Length == 0)
                {
                    return Json(new { success = false, message = "No notifications selected" });
                }

                // Get notifications that belong to the current user
                var notifications = _context.Notifications
                    .Where(n => notificationIds.Contains(n.NotificationId) && n.UserId == currentUser.UserId)
                    .ToList();

                if (notifications.Count == 0)
                {
                    return Json(new { success = false, message = "No valid notifications found" });
                }

                _context.Notifications.RemoveRange(notifications);
                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = $"Successfully deleted {notifications.Count} notification(s)",
                    deletedCount = notifications.Count
                });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = $"Error deleting notifications: {ex.Message}" });
            }
        }

        [HttpPost]
        public JsonResult DeleteAll()
        {
            try
            {
                var currentUser = _authService.GetCurrentUser();

                var notifications = _context.Notifications
                    .Where(n => n.UserId == currentUser.UserId)
                    .ToList();

                if (notifications.Count == 0)
                {
                    return Json(new { success = false, message = "No notifications to delete" });
                }

                _context.Notifications.RemoveRange(notifications);
                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = $"Successfully deleted all {notifications.Count} notification(s)",
                    deletedCount = notifications.Count
                });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = $"Error deleting notifications: {ex.Message}" });
            }
        }

        [HttpPost]
        public JsonResult DeleteRead()
        {
            try
            {
                var currentUser = _authService.GetCurrentUser();

                var notifications = _context.Notifications
                    .Where(n => n.UserId == currentUser.UserId && n.IsRead)
                    .ToList();

                if (notifications.Count == 0)
                {
                    return Json(new { success = false, message = "No read notifications to delete" });
                }

                _context.Notifications.RemoveRange(notifications);
                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = $"Successfully deleted {notifications.Count} read notification(s)",
                    deletedCount = notifications.Count
                });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = $"Error deleting notifications: {ex.Message}" });
            }
        }

        [HttpPost]
        public JsonResult DeleteOldest(int count)
        {
            try
            {
                var currentUser = _authService.GetCurrentUser();

                if (count <= 0)
                {
                    return Json(new { success = false, message = "Count must be greater than 0" });
                }

                // Get oldest notifications by creation date
                var notifications = _context.Notifications
                    .Where(n => n.UserId == currentUser.UserId)
                    .OrderBy(n => n.CreatedDate)
                    .Take(count)
                    .ToList();

                if (notifications.Count == 0)
                {
                    return Json(new { success = false, message = "No notifications to delete" });
                }

                _context.Notifications.RemoveRange(notifications);
                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = $"Successfully deleted {notifications.Count} oldest notification(s)",
                    deletedCount = notifications.Count
                });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = $"Error deleting notifications: {ex.Message}" });
            }
        }

        [HttpPost]
        public JsonResult DeleteOlderThan(string date)
        {
            try
            {
                var currentUser = _authService.GetCurrentUser();

                // Parse the date
                System.DateTime cutoffDate;
                if (!System.DateTime.TryParse(date, out cutoffDate))
                {
                    return Json(new { success = false, message = "Invalid date format" });
                }

                // Set time to end of day to include the entire cutoff day
                cutoffDate = cutoffDate.Date.AddDays(1).AddSeconds(-1);

                var notifications = _context.Notifications
                    .Where(n => n.UserId == currentUser.UserId && n.CreatedDate <= cutoffDate)
                    .ToList();

                if (notifications.Count == 0)
                {
                    return Json(new { success = false, message = "No notifications older than the specified date" });
                }

                _context.Notifications.RemoveRange(notifications);
                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = $"Successfully deleted {notifications.Count} notification(s) older than {cutoffDate.ToString("MMM dd, yyyy")}",
                    deletedCount = notifications.Count
                });
            }
            catch (System.Exception ex)
            {
                return Json(new { success = false, message = $"Error deleting notifications: {ex.Message}" });
            }
        }

        public JsonResult GetUnreadCount()
        {
            var currentUser = _authService.GetCurrentUser();
            var count = _notificationService.GetUnreadCount(currentUser.UserId);
            return Json(new { count = count }, JsonRequestBehavior.AllowGet);
        }

        public PartialViewResult GetRecentNotifications()
        {
            var currentUser = _authService.GetCurrentUser();
            var notifications = _notificationService.GetUserNotifications(currentUser.UserId, unreadOnly: true);
            return PartialView("_NotificationsList", notifications);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _notificationService?.Dispose();
                _authService?.Dispose();
                _context?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
