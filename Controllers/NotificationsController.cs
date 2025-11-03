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
