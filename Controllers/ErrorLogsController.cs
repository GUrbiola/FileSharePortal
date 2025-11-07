using System;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using FileSharePortal.Data;
using FileSharePortal.Models;
using FileSharePortal.Services;

namespace FileSharePortal.Controllers
{
    [Authorize]
    public class ErrorLogsController : Controller
    {
        private readonly FileSharePortalContext _context;
        private readonly AuthenticationService _authService;

        public ErrorLogsController()
        {
            _context = new FileSharePortalContext();
            _authService = new AuthenticationService();
        }

        private bool IsDebugAdmin()
        {
            var currentUser = _authService.GetCurrentUser();
            var debugAdminUsername = ConfigurationManager.AppSettings["DebugAdminUsername"];

            if (string.IsNullOrWhiteSpace(debugAdminUsername))
                return false;

            return currentUser.Username.Equals(debugAdminUsername, StringComparison.OrdinalIgnoreCase);
        }

        // GET: ErrorLogs
        public ActionResult Index(string filter = "all", int page = 1, int pageSize = 50)
        {
            if (!IsDebugAdmin())
            {
                return new HttpUnauthorizedResult();
            }

            var currentUser = _authService.GetCurrentUser();
            ViewBag.CurrentUser = currentUser;

            var query = _context.ErrorLogs.Include(e => e.User).AsQueryable();

            // Apply filters
            switch (filter)
            {
                case "unresolved":
                    query = query.Where(e => !e.IsResolved);
                    break;
                case "resolved":
                    query = query.Where(e => e.IsResolved);
                    break;
                case "today":
                    var today = DateTime.Today;
                    query = query.Where(e => e.OccurredAt >= today);
                    break;
                case "week":
                    var weekAgo = DateTime.Now.AddDays(-7);
                    query = query.Where(e => e.OccurredAt >= weekAgo);
                    break;
            }

            // Get total count for pagination
            var totalCount = query.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            // Apply pagination
            var errorLogs = query
                .OrderByDescending(e => e.OccurredAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.Filter = filter;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;
            ViewBag.PageSize = pageSize;

            return View(errorLogs);
        }

        // GET: ErrorLogs/Details/5
        public ActionResult Details(int id)
        {
            if (!IsDebugAdmin())
            {
                return new HttpUnauthorizedResult();
            }

            var currentUser = _authService.GetCurrentUser();
            ViewBag.CurrentUser = currentUser;

            var errorLog = _context.ErrorLogs
                .Include(e => e.User)
                .FirstOrDefault(e => e.ErrorLogId == id);

            if (errorLog == null)
            {
                return HttpNotFound();
            }

            return View(errorLog);
        }

        // POST: ErrorLogs/Resolve
        [HttpPost]
        public ActionResult Resolve(int id, string resolutionNotes)
        {
            if (!IsDebugAdmin())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var currentUser = _authService.GetCurrentUser();
            var errorLog = _context.ErrorLogs.Find(id);

            if (errorLog == null)
            {
                return Json(new { success = false, message = "Error log not found" });
            }

            errorLog.IsResolved = true;
            errorLog.ResolvedAt = DateTime.Now;
            errorLog.ResolvedByUserId = currentUser.UserId;
            errorLog.ResolutionNotes = resolutionNotes;

            _context.SaveChanges();

            return Json(new { success = true, message = "Error marked as resolved" });
        }

        // POST: ErrorLogs/Delete
        [HttpPost]
        public ActionResult Delete(int id)
        {
            if (!IsDebugAdmin())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var errorLog = _context.ErrorLogs.Find(id);

            if (errorLog == null)
            {
                return Json(new { success = false, message = "Error log not found" });
            }

            _context.ErrorLogs.Remove(errorLog);
            _context.SaveChanges();

            return Json(new { success = true, message = "Error log deleted" });
        }

        // POST: ErrorLogs/DeleteAll
        [HttpPost]
        public ActionResult DeleteAll(string filter = "resolved")
        {
            if (!IsDebugAdmin())
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var query = _context.ErrorLogs.AsQueryable();

            if (filter == "resolved")
            {
                query = query.Where(e => e.IsResolved);
            }
            else if (filter == "old")
            {
                var thirtyDaysAgo = DateTime.Now.AddDays(-30);
                query = query.Where(e => e.OccurredAt < thirtyDaysAgo);
            }
            else if(filter == "all")
            {
                // No additional filtering
            }



            var logsToDelete = query.ToList();
            var count = logsToDelete.Count;

            _context.ErrorLogs.RemoveRange(logsToDelete);
            _context.SaveChanges();

            return Json(new { success = true, message = $"{count} error logs deleted" });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
