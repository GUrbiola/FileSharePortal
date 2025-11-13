using System;
using System.EnterpriseServices.Internal;
using System.IO;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using FileSharePortal.Helpers;
using FileSharePortal.Models;
using FileSharePortal.Services;
using log4net;

namespace FileSharePortal
{
    public class MvcApplication : System.Web.HttpApplication
    {
        private static readonly ILog Logger = LoggingHelper.GetLogger(typeof(MvcApplication));
        private static DateTime _lastRedirectTime = DateTime.MinValue;
        private static int RedirectCount = 0;

        protected void Application_Start()
        {
            // Initialize log4net
            log4net.Config.XmlConfigurator.Configure();
            

            // Ensure Logs directory exists
            string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logDirectory))
            {
                Directory.CreateDirectory(logDirectory);
            }

            Logger.Info("========== Application Starting ==========");
            Logger.Info($"Application Base Directory: {AppDomain.CurrentDomain.BaseDirectory}");
            Logger.Info($"Log Directory: {logDirectory}");

            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            Logger.Info("========== Application Started Successfully ==========");
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            Exception exception = null;
            int redirectTimer = 100;

            try
            {
                exception = Server.GetLastError();

                if (exception == null)
                    return;

                // Get the base exception (unwrap HttpException if present)
                var baseException = exception.GetBaseException();

                // Log to log4net
                Logger.Error("=== UNHANDLED EXCEPTION ===");
                Logger.Error($"Type: {baseException.GetType().FullName}");
                Logger.Error($"Message: {baseException.Message}");
                Logger.Error($"Stack Trace: {baseException.StackTrace}");
                if (baseException.InnerException != null)
                {
                    Logger.Error($"Inner Exception: {baseException.InnerException.Message}");
                }
                Logger.Error("========================");

                // Log to trace/event log FIRST - before any database operations
                System.Diagnostics.Trace.TraceError($"=== UNHANDLED EXCEPTION ===");
                System.Diagnostics.Trace.TraceError($"Type: {baseException.GetType().FullName}");
                System.Diagnostics.Trace.TraceError($"Message: {baseException.Message}");
                System.Diagnostics.Trace.TraceError($"Stack Trace: {baseException.StackTrace}");
                if (baseException.InnerException != null)
                {
                    System.Diagnostics.Trace.TraceError($"Inner Exception: {baseException.InnerException.Message}");
                }
                System.Diagnostics.Trace.TraceError($"========================");



                // Try to log error to database (but don't fail if this doesn't work)
                try
                {
                    using (var errorLoggingService = new ErrorLoggingService())
                    {
                        var errorId = errorLoggingService.LogError(baseException, Context);
                        System.Diagnostics.Trace.TraceInformation($"Error logged to database with ID: {errorId}");
                    }

                }
                catch (Exception dbEx)
                {
                    // Database logging failed - log this but continue
                    System.Diagnostics.Trace.TraceError($"Failed to log error to database: {dbEx.Message}");
                    System.Diagnostics.Trace.TraceError($"Database error stack trace: {dbEx.StackTrace}");
                }

                //var httpException = exception as HttpException;
                //if (httpException != null)
                //{
                //    if (httpException.GetHttpCode() == 404)
                //    {
                //        Server.ClearError();
                //        RedirectIfMoreThanMillisecondsPassed("~/Error/NotFound", redirectTimer);
                //    }
                //    else if (httpException.GetHttpCode() == 403)
                //    {
                //        Server.ClearError();
                //        RedirectIfMoreThanMillisecondsPassed("~/Error/Unauthorized", redirectTimer);
                //    }
                //    else
                //    {
                //        Server.ClearError();
                //        RedirectIfMoreThanMillisecondsPassed("~/Error/Index", redirectTimer);
                //    }
                //    return;
                //}
                //else
                //{
                //    // Clear the error
                //    Server.ClearError();
                //    RedirectIfMoreThanMillisecondsPassed("~/Error/Index", redirectTimer);
                //}
            }
            catch (Exception ex)
            {
                // If error handling fails, log it
                System.Diagnostics.Trace.TraceError($"=== ERROR HANDLER FAILED ===");
                System.Diagnostics.Trace.TraceError($"Handler Error: {ex.Message}");
                System.Diagnostics.Trace.TraceError($"Handler Stack Trace: {ex.StackTrace}");
                if (exception != null)
                {
                    System.Diagnostics.Trace.TraceError($"Original Error: {exception.Message}");
                }
                System.Diagnostics.Trace.TraceError($"========================");
            }
        }

        private void RedirectIfMoreThanMillisecondsPassed(string url, int milliseconds)
        {
            var _notificationService = new NotificationService();
            int millisecondsSinceLastRedirect = _lastRedirectTime == DateTime.MinValue ? 0 : (int)(DateTime.Now - _lastRedirectTime).TotalMilliseconds;
            if(RedirectCount > 20)
            {
                RedirectCount = 0;
                _lastRedirectTime = DateTime.Now.AddSeconds(10); // Pause redirects for 10 seconds
                return;
            }
            else
            {
                if (_lastRedirectTime == DateTime.MinValue || (DateTime.Now - _lastRedirectTime) > TimeSpan.FromMilliseconds(milliseconds))
                {
                    RedirectCount++;
                    _notificationService.CreateNotification(2, "Trying to Redirect", $"Attempt to redirect response, {millisecondsSinceLastRedirect} ms since last redirect", NotificationType.ApplicationAlert);
                    _lastRedirectTime = DateTime.Now;
                    Response.Redirect(url);
                }
            }
        }
    }
}
