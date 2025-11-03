using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Web.Mvc;
using FileSharePortal.Data;
using FileSharePortal.Models;
using FileSharePortal.Services;

namespace FileSharePortal.Controllers
{
    public class ApplicationsController : Controller
    {
        private readonly FileSharePortalContext _context;
        private readonly AuthenticationService _authService;

        public ApplicationsController()
        {
            _context = new FileSharePortalContext();
            _authService = new AuthenticationService();
        }

        public ActionResult Index()
        {
            var currentUser = _authService.GetCurrentUser();
            ViewBag.CurrentUser = currentUser;

            var applications = _context.Applications
                .Where(a => a.IsActive)
                .OrderBy(a => a.ApplicationName)
                .ToList();

            return View(applications);
        }

        public ActionResult Details(int id)
        {
            var currentUser = _authService.GetCurrentUser();
            ViewBag.CurrentUser = currentUser;

            var application = _context.Applications.Find(id);
            if (application == null)
            {
                return HttpNotFound();
            }

            var executions = _context.ApplicationExecutions
                .Where(e => e.ApplicationId == id)
                .OrderByDescending(e => e.StartTime)
                .Take(50)
                .ToList();

            ViewBag.Executions = executions;

            return View(application);
        }

        [HttpPost]
        public JsonResult CheckStatus(int id)
        {
            var application = _context.Applications.Find(id);
            if (application == null)
            {
                return Json(new { success = false, message = "Application not found" });
            }

            try
            {
                ApplicationStatus status = ApplicationStatus.Unknown;

                if (!string.IsNullOrEmpty(application.StatusEndpoint))
                {
                    using (var client = new HttpClient())
                    {
                        client.Timeout = TimeSpan.FromSeconds(10);
                        var response = client.GetAsync(application.StatusEndpoint).Result;

                        if (response.IsSuccessStatusCode)
                        {
                            status = ApplicationStatus.Running;
                            application.LastSuccessfulRun = DateTime.Now;
                        }
                        else
                        {
                            status = ApplicationStatus.Error;
                        }
                    }
                }

                application.CurrentStatus = status;
                application.LastStatusCheck = DateTime.Now;
                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    status = status.ToString(),
                    lastCheck = application.LastStatusCheck?.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
            catch (Exception ex)
            {
                application.CurrentStatus = ApplicationStatus.Error;
                application.LastStatusCheck = DateTime.Now;
                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    status = ApplicationStatus.Error.ToString(),
                    message = ex.Message
                });
            }
        }

        public ActionResult DownloadLog(int id)
        {
            var application = _context.Applications.Find(id);
            if (application == null || string.IsNullOrEmpty(application.LogPath))
            {
                return HttpNotFound();
            }

            var logPath = Server.MapPath(application.LogPath);
            if (!System.IO.File.Exists(logPath))
            {
                return HttpNotFound("Log file not found.");
            }

            var fileName = Path.GetFileName(logPath);
            return File(logPath, "text/plain", fileName);
        }

        public JsonResult GetExecutionHistory(int id, int days = 7)
        {
            var startDate = DateTime.Now.AddDays(-days);
            var executions = _context.ApplicationExecutions
                .Where(e => e.ApplicationId == id && e.StartTime >= startDate)
                .OrderBy(e => e.StartTime)
                .Select(e => new
                {
                    startTime = e.StartTime,
                    endTime = e.EndTime,
                    status = e.Status.ToString(),
                    recordsProcessed = e.RecordsProcessed
                })
                .ToList();

            return Json(executions, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult Create()
        {
            var currentUser = _authService.GetCurrentUser();
            if (!currentUser.IsAdmin)
            {
                return new HttpUnauthorizedResult();
            }

            ViewBag.CurrentUser = currentUser;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(string applicationName, string description, string statusEndpoint, string logPath, int checkIntervalMinutes)
        {
            var currentUser = _authService.GetCurrentUser();
            if (!currentUser.IsAdmin)
            {
                return new HttpUnauthorizedResult();
            }

            var application = new Models.Application
            {
                ApplicationName = applicationName,
                Description = description,
                StatusEndpoint = statusEndpoint,
                LogPath = logPath,
                CheckIntervalMinutes = checkIntervalMinutes,
                IsActive = true
            };

            _context.Applications.Add(application);
            _context.SaveChanges();

            TempData["Success"] = "Application added successfully!";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public ActionResult Edit(int id)
        {
            var currentUser = _authService.GetCurrentUser();
            if (!currentUser.IsAdmin)
            {
                return new HttpUnauthorizedResult();
            }

            ViewBag.CurrentUser = currentUser;
            var application = _context.Applications.Find(id);
            if (application == null)
            {
                return HttpNotFound();
            }

            return View(application);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, string applicationName, string description, string statusEndpoint, string logPath, int checkIntervalMinutes, bool isActive)
        {
            var currentUser = _authService.GetCurrentUser();
            if (!currentUser.IsAdmin)
            {
                return new HttpUnauthorizedResult();
            }

            var application = _context.Applications.Find(id);
            if (application == null)
            {
                return HttpNotFound();
            }

            application.ApplicationName = applicationName;
            application.Description = description;
            application.StatusEndpoint = statusEndpoint;
            application.LogPath = logPath;
            application.CheckIntervalMinutes = checkIntervalMinutes;
            application.IsActive = isActive;

            _context.SaveChanges();

            TempData["Success"] = "Application updated successfully!";
            return RedirectToAction("Details", new { id = id });
        }

        [HttpPost]
        public JsonResult Delete(int id)
        {
            try
            {
                var currentUser = _authService.GetCurrentUser();
                if (!currentUser.IsAdmin)
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                var application = _context.Applications.Find(id);
                if (application == null)
                {
                    return Json(new { success = false, message = "Application not found" });
                }

                // Delete all executions for this application
                var executions = _context.ApplicationExecutions.Where(e => e.ApplicationId == id).ToList();
                foreach (var execution in executions)
                {
                    // Delete log files for this execution
                    var logFiles = _context.ApplicationLogFiles.Where(l => l.ExecutionId == execution.ExecutionId).ToList();
                    _context.ApplicationLogFiles.RemoveRange(logFiles);
                }
                _context.ApplicationExecutions.RemoveRange(executions);

                // Delete API tokens associated with this application
                var tokens = _context.ApiTokens.Where(t => t.ApplicationId == id).ToList();
                _context.ApiTokens.RemoveRange(tokens);

                // Delete the application
                _context.Applications.Remove(application);
                _context.SaveChanges();

                return Json(new { success = true, message = $"Application '{application.ApplicationName}' deleted successfully." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Error deleting application: {ex.Message}" });
            }
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
