using System;
using System.Collections.Generic;
using System.Linq;
using FileSharePortal.Data;
using FileSharePortal.Helpers;
using FileSharePortal.Models;
using log4net;

namespace FileSharePortal.Services
{
    public class NotificationService
    {
        private static readonly ILog Logger = LoggingHelper.GetLogger(typeof(NotificationService));
        private readonly FileSharePortalContext _context;

        public NotificationService()
        {
            _context = new FileSharePortalContext();
        }

        public void CreateNotification(int userId, string title, string message, NotificationType type, int? relatedFileId = null, int? relatedApplicationId = null, 
            string actionUrl = null)
        {
            LoggingHelper.LogWithParams(Logger, "CreateNotification", userId, title, message, type, relatedFileId, relatedApplicationId, actionUrl);
            Logger.Info($"Creating notification for user {userId}: {title}");

            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type,
                    RelatedFileId = relatedFileId,
                    RelatedApplicationId = relatedApplicationId,
                    ActionUrl = actionUrl
                };

                _context.Notifications.Add(notification);

                _context.SaveChanges();

                Logger.Info($"Notification created successfully with ID: {notification.NotificationId}");
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, $"Error creating notification for user {userId}", ex);
                throw;
            }
        }

        public void NotifyAdmins(string title, string message, NotificationType type, int? relatedFileId = null, string actionUrl = null)
        {
            Logger.Info($"Notifying admins: {title}");
            try
            {
                var admins = _context.Users.Where(u => u.IsAdmin && u.IsActive).ToList();
                Logger.Debug($"Found {admins.Count} active admin(s)");

                foreach (var admin in admins)
                {
                    Logger.Debug($"Creating notification for admin: {admin.Username} (UserId: {admin.UserId})");
                    CreateNotification(admin.UserId, title, message, type, relatedFileId, actionUrl: actionUrl);
                }

                Logger.Info($"Successfully notified {admins.Count} admin(s)");
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, "Error notifying admins", ex);
                throw;
            }
        }

        public void NotifyFileShared(int fileId, List<int> userIds, int sharedByUserId)
        {
            Logger.Info($"Notifying users about shared file {fileId} by user {sharedByUserId}");

            try
            {
                var file = _context.SharedFiles.Find(fileId);

                var sharedBy = _context.Users.Find(sharedByUserId);

                if (file != null && sharedBy != null)
                {
                    Logger.Debug($"File found: {file.FileName}, Shared by: {sharedBy.FullName}");

                    int notifiedCount = 0;
                    foreach (var userId in userIds)
                    {
                        // Don't notify the person who shared it
                        if (userId == sharedByUserId)
                            continue;

                        CreateNotification(
                            userId,
                            "File Shared With You",
                            $"{sharedBy.FullName} has shared the file '{file.FileName}' with you.",
                            NotificationType.FileShared,
                            fileId,
                            actionUrl: $"/Files/Details/{fileId}"
                        );
                        notifiedCount++;
                    }

                    Logger.Info($"Successfully notified {notifiedCount} user(s) about shared file");
                }
                else
                {
                    Logger.Warn($"File or user not found - FileId: {fileId}, SharedByUserId: {sharedByUserId}");
                }

            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, $"Error notifying users about shared file {fileId}", ex);
                throw;
            }
        }

        public void NotifyFileAccessRemoved(int fileId, List<int> userIds, int removedByUserId)
        {
            Logger.Info($"Notifying users about removed access to file {fileId} by user {removedByUserId}");

            try
            {
                var file = _context.SharedFiles.Find(fileId);

                var removedBy = _context.Users.Find(removedByUserId);

                if (file != null && removedBy != null)
                {
                    int notifiedCount = 0;
                    foreach (var userId in userIds)
                    {
                        // Don't notify the person who removed access
                        if (userId == removedByUserId)
                            continue;

                        CreateNotification(
                            userId,
                            "File Access Removed",
                            $"Your access to the file '{file.FileName}' has been removed.",
                            NotificationType.General,
                            fileId
                        );
                        notifiedCount++;
                    }

                    Logger.Info($"Successfully notified {notifiedCount} user(s) about removed file access");
                }
                else
                {
                    Logger.Warn($"File or user not found - FileId: {fileId}, RemovedByUserId: {removedByUserId}");
                }

            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, $"Error notifying users about removed file access {fileId}", ex);
                throw;
            }
        }

        public void NotifyFileReported(int reportId)
        {
            Logger.Info($"Notifying admins about reported file - ReportId: {reportId}");

            try
            {
                var report = _context.FileReports.Find(reportId);

                if (report != null)
                {
                    var file = _context.SharedFiles.Find(report.FileId);
                    var reporter = _context.Users.Find(report.ReportedByUserId);

                    if (file != null && reporter != null)
                    {
                        NotifyAdmins(
                            "File Reported",
                            $"{reporter.FullName} has reported the file '{file.FileName}'. Reason: {report.Reason}",
                            NotificationType.FileReported,
                            file.FileId,
                            $"/Admin/ReviewReport/{reportId}"
                        );

                        Logger.Info($"Successfully notified admins about file report {reportId}");
                    }
                    else
                    {
                        Logger.Warn($"File or reporter not found for report {reportId}");
                    }
                }
                else
                {
                    Logger.Warn($"File report not found with ID: {reportId}");
                }

            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, $"Error notifying admins about file report {reportId}", ex);
                throw;
            }
        }

        public List<Notification> GetUserNotifications(int userId, bool unreadOnly = false)
        {
            Logger.Info($"Retrieving notifications for user {userId} (UnreadOnly: {unreadOnly})");

            try
            {
                var query = _context.Notifications.Where(n => n.UserId == userId);

                if (unreadOnly)
                {
                    query = query.Where(n => !n.IsRead);
                }

                var notifications = query.OrderByDescending(n => n.CreatedDate).ToList();

                Logger.Info($"Retrieved {notifications.Count} notification(s) for user {userId}");
                return notifications;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, $"Error retrieving notifications for user {userId}", ex);
                throw;
            }
        }

        public int GetUnreadCount(int userId)
        {
            Logger.Debug($"Retrieving unread notification count for user {userId}");

            try
            {
                var count = _context.Notifications.Count(n => n.UserId == userId && !n.IsRead);

                Logger.Debug($"User {userId} has {count} unread notification(s)");
                return count;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, $"Error retrieving unread count for user {userId}", ex);
                throw;
            }
        }

        public void MarkAsRead(int notificationId)
        {
            Logger.Info($"Marking notification {notificationId} as read");

            try
            {
                var notification = _context.Notifications.Find(notificationId);

                if (notification != null && !notification.IsRead)
                {
                    Logger.Debug($"Notification found - UserId: {notification.UserId}, Title: {notification.Title}");

                    notification.IsRead = true;
                    notification.ReadDate = DateTime.Now;

                    _context.SaveChanges();

                    Logger.Info($"Notification {notificationId} marked as read successfully");
                }
                else if (notification == null)
                {
                    Logger.Warn($"Notification not found with ID: {notificationId}");
                }
                else
                {
                    Logger.Debug($"Notification {notificationId} was already marked as read");
                }

            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, $"Error marking notification {notificationId} as read", ex);
                throw;
            }
        }

        public void MarkAllAsRead(int userId)
        {
            Logger.Info($"Marking all notifications as read for user {userId}");

            try
            {
                var notifications = _context.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToList();
                Logger.Debug($"Found {notifications.Count} unread notification(s)");

                if (notifications.Count > 0)
                {
                    foreach (var notification in notifications)
                    {
                        notification.IsRead = true;
                        notification.ReadDate = DateTime.Now;
                    }

                    _context.SaveChanges();

                    Logger.Info($"Successfully marked {notifications.Count} notification(s) as read for user {userId}");
                }
                else
                {
                    Logger.Debug($"No unread notifications found for user {userId}");
                }

            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, $"Error marking all notifications as read for user {userId}", ex);
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                _context?.Dispose();
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, "Error during dispose", ex);
                throw;
            }
        }

        internal void NotifyFileDeleted(int fileId, List<int> usersWithAccess, int userId)
        {
            Logger.Info($"Notifying users about deleted file {fileId} by user {userId}");

            try
            {
                var file = _context.SharedFiles.Find(fileId);
                var removedBy = _context.Users.Find(userId);

                if (file != null && removedBy != null)
                {
                    int notifiedCount = 0;
                    foreach (var usr in usersWithAccess)
                    {
                        // Don't notify the person who removed access
                        if (usr == userId)
                            continue;

                        CreateNotification(
                            usr,
                            "File Access Removed",
                            $"Your access to the file '{file.FileName}' has been removed.",
                            NotificationType.General,
                            fileId
                        );
                        notifiedCount++;
                    }

                    Logger.Info($"Successfully notified {notifiedCount} user(s) about deleted file");
                }
                else
                {
                    Logger.Warn($"File or user not found - FileId: {fileId}, UserId: {userId}");
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, $"Error notifying users about deleted file {fileId}", ex);
                throw;
            }
        }
    }
}
