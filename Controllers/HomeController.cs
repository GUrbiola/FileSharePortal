using System.Linq;
using System.Web.Mvc;
using FileSharePortal.Data;
using FileSharePortal.Services;

namespace FileSharePortal.Controllers
{
    public class HomeController : Controller
    {
        private readonly FileSharePortalContext _context;
        private readonly AuthenticationService _authService;

        public HomeController()
        {
            _context = new FileSharePortalContext();
            _authService = new AuthenticationService();
        }

        public ActionResult Index()
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null)
                return RedirectToAction("Login", "Account");

            ViewBag.CurrentUser = currentUser;

            // Get recent files shared with the user
            var recentFiles = _context.FileShares
                .Where(fs => fs.SharedWithUserId == currentUser.UserId && !fs.SharedFile.IsDeleted)
                .OrderByDescending(fs => fs.SharedDate)
                .Take(5)
                .Select(fs => fs.SharedFile)
                .ToList();

            ViewBag.RecentFiles = recentFiles;

            // Get user's uploaded files count
            ViewBag.UploadedFilesCount = _context.SharedFiles
                .Count(f => f.UploadedByUserId == currentUser.UserId && !f.IsDeleted);

            // Get shared files count
            int sharedWithMe = _context.FileShares
                .Count(fs => fs.SharedWithUserId == currentUser.UserId && !fs.SharedFile.IsDeleted);
            int sharedWithMyRole = _context.RoleUsers
                .Where(ru => ru.UserId == currentUser.UserId)
                .Join(_context.FileShares,
                      ru => ru.RoleId,
                      fs => fs.SharedWithRoleId,
                      (ru, fs) => fs.SharedFile)
                .Count(f => !f.IsDeleted);  

            ViewBag.SharedFilesCount = sharedWithMe+ sharedWithMyRole;

            return View();
        }

        public ActionResult About()
        {
            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context?.Dispose();
                _authService?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
