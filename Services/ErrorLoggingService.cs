using System;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using FileSharePortal.Data;
using FileSharePortal.Helpers;
using FileSharePortal.Models;
using log4net;

namespace FileSharePortal.Services
{
    public class ErrorLoggingService : IDisposable
    {
        private static readonly ILog Logger = LoggingHelper.GetLogger(typeof(ErrorLoggingService));
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
            LoggingHelper.LogWithParams(Logger, "LogError", exception?.GetType().FullName, httpContext != null);
            Logger.Error($"Logging error: {exception?.Message}", exception);

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
                    Logger.Debug("HTTP context available, extracting request information");

                    try
                    {
                        errorLog.RequestUrl = httpContext.Request.Url?.ToString();
                        errorLog.HttpMethod = httpContext.Request.HttpMethod;
                        errorLog.UserAgent = httpContext.Request.UserAgent;
                        errorLog.IpAddress = GetClientIpAddress(httpContext);

                        Logger.Debug($"Request info - URL: {errorLog.RequestUrl}, Method: {errorLog.HttpMethod}");

                        // Try to get current user information
                        if (httpContext.User?.Identity?.IsAuthenticated == true)
                        {
                            errorLog.Username = httpContext.User.Identity.Name;

                            // Try to get UserId from database
                            var user = _context.Users.FirstOrDefault(u => u.Username == errorLog.Username);
                            if (user != null)
                            {
                                errorLog.UserId = user.UserId;
                                Logger.Debug($"User identified - Username: {errorLog.Username}, UserId: {errorLog.UserId}");
                            }
                        }

                        // Try to get route data for controller and action
                        var routeData = httpContext.Request.RequestContext?.RouteData;
                        if (routeData != null)
                        {
                            errorLog.ControllerName = routeData.Values["controller"]?.ToString();
                            errorLog.ActionName = routeData.Values["action"]?.ToString();
                            Logger.Debug($"Route data - Controller: {errorLog.ControllerName}, Action: {errorLog.ActionName}");
                        }
                    }
                    catch (Exception contextEx)
                    {
                        // If we can't get context info, just continue without it
                        Logger.Warn($"Failed to extract HTTP context info: {contextEx.Message}");
                    }
                }
                else
                {
                    Logger.Debug("No HTTP context available");
                }

                // Save error to database
                _context.ErrorLogs.Add(errorLog);
                _context.SaveChanges();

                Logger.Info($"Error logged to database with ID: {errorLog.ErrorLogId}");

                // Notify debug admin if configured
                NotifyDebugAdmin(errorLog);

                return errorLog.ErrorLogId;
            }
            catch (Exception ex)
            {
                // If we can't log to database, at least log to trace/event log
                Logger.Error("Error logging to database failed", ex);
                System.Diagnostics.Trace.WriteLine($"Error logging failed: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// Notifies the debug admin user if configured in web.config
        /// </summary>
        private void NotifyDebugAdmin(ErrorLog errorLog)
        {
            Logger.Debug("Attempting to notify debug admin of error");

            try
            {
                var debugAdminUsername = ConfigurationManager.AppSettings["DebugAdminUsername"];

                if (string.IsNullOrWhiteSpace(debugAdminUsername))
                {
                    Logger.Debug("DebugAdminUsername not configured, skipping notification");
                    return;
                }

                Logger.Debug($"Debug admin username configured: {debugAdminUsername}");

                // Find the debug admin user
                var debugAdmin = _context.Users.FirstOrDefault(u =>
                    u.Username.Equals(debugAdminUsername, StringComparison.OrdinalIgnoreCase) &&
                    u.IsActive);

                if (debugAdmin == null)
                {
                    Logger.Warn($"Debug admin user not found or inactive: {debugAdminUsername}");
                    return;
                }

                Logger.Debug($"Debug admin found - UserId: {debugAdmin.UserId}");

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

                Logger.Info($"Debug admin notification created successfully for error {errorLog.ErrorLogId}");
            }
            catch (Exception ex)
            {
                // If notification fails, just log it and continue
                LoggingHelper.LogError(Logger, "Debug admin notification failed", ex);
                System.Diagnostics.Trace.WriteLine($"Debug admin notification failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Extracts source file and line number from stack trace
        /// </summary>
        private void ExtractSourceLocation(string stackTrace, ErrorLog errorLog)
        {
            Logger.Debug("Extracting source location from stack trace");

            if (string.IsNullOrWhiteSpace(stackTrace))
            {
                Logger.Debug("Stack trace is empty, nothing to extract");
                return;
            }

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

                    Logger.Debug($"Source location extracted - File: {errorLog.SourceFile}, Line: {errorLog.LineNumber}");
                }
                else
                {
                    Logger.Debug("No source location pattern matched in stack trace");
                }

            }
            catch (Exception ex)
            {
                // If extraction fails, just continue without source location
                Logger.Warn($"Failed to extract source location: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the client IP address from the HTTP context
        /// </summary>
        private string GetClientIpAddress(HttpContext context)
        {
            Logger.Debug("Extracting client IP address");

            try
            {
                var ipAddress = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

                if (!string.IsNullOrEmpty(ipAddress))
                {
                    Logger.Debug($"X-Forwarded-For header found: {ipAddress}");
                    var addresses = ipAddress.Split(',');
                    if (addresses.Length != 0)
                    {
                        var clientIp = addresses[0];
                        Logger.Debug($"Client IP extracted: {clientIp}");
                        return clientIp;
                    }
                }

                var remoteAddr = context.Request.ServerVariables["REMOTE_ADDR"];
                Logger.Debug($"Remote address: {remoteAddr}");
                return remoteAddr;
            }
            catch (Exception ex)
            {
                Logger.Warn($"Failed to get client IP address: {ex.Message}");
                return "Unknown";
            }
        }

        public void Dispose()
        {
            try
            {
                _context?.Dispose();
                _notificationService?.Dispose();
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, "Error during dispose", ex);
                throw;
            }
        }
    }
}
