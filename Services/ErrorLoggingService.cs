using System;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using FileSharePortal.Data;
using FileSharePortal.Models;

namespace FileSharePortal.Services
{
    public class ErrorLoggingService : IDisposable
    {
        private readonly FileSharePortalContext _context;
        private readonly NotificationService _notificationService;

        public ErrorLoggingService()
        {
            _context = new FileSharePortalContext();
            _notificationService = new NotificationService();
        }

        /// <summary>
        /// Logs an exception to the database and notifies the debug admin if configured
        /// </summary>
        public int LogError(Exception exception, HttpContext httpContext = null)
        {
            try
            {
                var errorLog = new ErrorLog
                {
                    ErrorMessage = exception.Message,
                    InnerException = exception.InnerException?.Message,
                    StackTrace = exception.StackTrace,
                    ExceptionType = exception.GetType().FullName,
                    OccurredAt = DateTime.Now
                };

                // Extract source file and line number from stack trace
                ExtractSourceLocation(exception.StackTrace, errorLog);

                // Get HTTP context information if available
                if (httpContext != null)
                {
                    try
                    {
                        errorLog.RequestUrl = httpContext.Request.Url?.ToString();
                        errorLog.HttpMethod = httpContext.Request.HttpMethod;
                        errorLog.UserAgent = httpContext.Request.UserAgent;
                        errorLog.IpAddress = GetClientIpAddress(httpContext);

                        // Try to get current user information
                        if (httpContext.User?.Identity?.IsAuthenticated == true)
                        {
                            errorLog.Username = httpContext.User.Identity.Name;

                            // Try to get UserId from database
                            var user = _context.Users.FirstOrDefault(u => u.Username == errorLog.Username);
                            if (user != null)
                            {
                                errorLog.UserId = user.UserId;
                            }
                        }

                        // Try to get route data for controller and action
                        var routeData = httpContext.Request.RequestContext?.RouteData;
                        if (routeData != null)
                        {
                            errorLog.ControllerName = routeData.Values["controller"]?.ToString();
                            errorLog.ActionName = routeData.Values["action"]?.ToString();
                        }
                    }
                    catch
                    {
                        // If we can't get context info, just continue without it
                    }
                }

                // Save error to database
                _context.ErrorLogs.Add(errorLog);
                _context.SaveChanges();

                // Notify debug admin if configured
                NotifyDebugAdmin(errorLog);

                return errorLog.ErrorLogId;
            }
            catch (Exception ex)
            {
                // If we can't log to database, at least log to trace/event log
                System.Diagnostics.Trace.WriteLine($"Error logging failed: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Notifies the debug admin user if configured in web.config
        /// </summary>
        private void NotifyDebugAdmin(ErrorLog errorLog)
        {
            try
            {
                var debugAdminUsername = ConfigurationManager.AppSettings["DebugAdminUsername"];

                if (string.IsNullOrWhiteSpace(debugAdminUsername))
                {
                    return;
                }

                // Find the debug admin user
                var debugAdmin = _context.Users.FirstOrDefault(u =>
                    u.Username.Equals(debugAdminUsername, StringComparison.OrdinalIgnoreCase) &&
                    u.IsActive);

                if (debugAdmin == null)
                {
                    return;
                }

                // Create notification for debug admin
                var notification = new Notification
                {
                    UserId = debugAdmin.UserId,
                    Title = "Application Error Occurred",
                    Message = $"An error occurred in the application.\n\n" +
                             $"Error: {errorLog.ErrorMessage}\n" +
                             $"Type: {errorLog.ExceptionType}\n" +
                             $"Time: {errorLog.OccurredAt:yyyy-MM-dd HH:mm:ss}\n" +
                             (errorLog.ControllerName != null ? $"Location: {errorLog.ControllerName}/{errorLog.ActionName}\n" : "") +
                             (errorLog.Username != null ? $"User: {errorLog.Username}\n" : "") +
                             $"Error ID: {errorLog.ErrorLogId}",
                    Type = NotificationType.Error,
                    IsRead = false,
                    CreatedDate = DateTime.Now
                };

                _context.Notifications.Add(notification);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                // If notification fails, just log it and continue
                System.Diagnostics.Trace.WriteLine($"Debug admin notification failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Extracts source file and line number from stack trace
        /// </summary>
        private void ExtractSourceLocation(string stackTrace, ErrorLog errorLog)
        {
            if (string.IsNullOrWhiteSpace(stackTrace))
                return;

            try
            {
                // Pattern to match: "in C:\path\to\file.cs:line 123"
                var pattern = @"in (.+?):line (\d+)";
                var match = Regex.Match(stackTrace, pattern);

                if (match.Success)
                {
                    errorLog.SourceFile = match.Groups[1].Value;
                    if (int.TryParse(match.Groups[2].Value, out int lineNumber))
                    {
                        errorLog.LineNumber = lineNumber;
                    }
                }
            }
            catch
            {
                // If extraction fails, just continue without source location
            }
        }

        /// <summary>
        /// Gets the client IP address from the HTTP context
        /// </summary>
        private string GetClientIpAddress(HttpContext context)
        {
            try
            {
                var ipAddress = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

                if (!string.IsNullOrEmpty(ipAddress))
                {
                    var addresses = ipAddress.Split(',');
                    if (addresses.Length != 0)
                    {
                        return addresses[0];
                    }
                }

                return context.Request.ServerVariables["REMOTE_ADDR"];
            }
            catch
            {
                return "Unknown";
            }
        }

        public void Dispose()
        {
            _context?.Dispose();
            _notificationService?.Dispose();
        }
    }
}
