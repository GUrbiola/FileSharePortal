using System;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using FileSharePortal.Services;

namespace FileSharePortal
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        protected void Application_Error(object sender, EventArgs e)
        {
            Exception exception = null;

            try
            {
                exception = Server.GetLastError();

                if (exception == null)
                    return;

                // Get the base exception (unwrap HttpException if present)
                var baseException = exception.GetBaseException();

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

                // Don't log 404 errors (Not Found)
                //var httpException = exception as HttpException;
                //if (httpException != null)
                //{
                //    if (httpException.GetHttpCode() == 404)
                //    {
                //        Server.ClearError();
                //        Response.Redirect("~/Error/NotFound");
                //    }
                //    else if (httpException.GetHttpCode() == 403)
                //    {
                //        Server.ClearError();
                //        Response.Redirect("~/Error/Unauthorized");
                //    }
                //    else
                //    {
                //        Server.ClearError();
                //        Response.Redirect("~/Error/Index");
                //    }
                //    return;
                //}
                //else
                //{
                //    // Clear the error
                //    Server.ClearError();
                //    Response.Redirect("~/Error/Index");
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
    }
}
