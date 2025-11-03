using System;
using System.Collections.Generic;
using System.Linq;
using FileSharePortal.Data;
using FileSharePortal.Models;

namespace FileSharePortal.Services
{
    public class NotificationService
    {
        private readonly FileSharePortalContext _context;

        public NotificationService()
        {
            _context = new FileSharePortalContext();
        }

        public void CreateNotification(int userId, string title, string message, NotificationType type,
            int? relatedFileId = null, int? relatedApplicationId = null, string actionUrl = null)
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
        }

        public void NotifyAdmins(string title, string message, NotificationType type,
            int? relatedFileId = null, string actionUrl = null)
        {
            var admins = _context.Users.Where(u => u.IsAdmin && u.IsActive).ToList();

            foreach (var admin in admins)
            {
                CreateNotification(admin.UserId, title, message, type, relatedFileId, actionUrl: actionUrl);
            }
        }

        public void NotifyFileShared(int fileId, List<int> userIds, int sharedByUserId)
        {
            var file = _context.SharedFiles.Find(fileId);
            var sharedBy = _context.Users.Find(sharedByUserId);

            if (file != null && sharedBy != null)
            {
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
                }
            }
        }

        public void NotifyFileAccessRemoved(int fileId, List<int> userIds, int removedByUserId)
        {
            var file = _context.SharedFiles.Find(fileId);
            var removedBy = _context.Users.Find(removedByUserId);

            if (file != null && removedBy != null)
            {
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
                }
            }
        }

        public void NotifyFileReported(int reportId)
        {
            var report = _context.FileReports.Find(reportId);
            if (report != null)
            {
                var file = _context.SharedFiles.Find(report.FileId);
                var reporter = _context.Users.Find(report.ReportedByUserId);

                NotifyAdmins(
                    "File Reported",
                    $"{reporter.FullName} has reported the file '{file.FileName}'. Reason: {report.Reason}",
                    NotificationType.FileReported,
                    file.FileId,
                    $"/Admin/ReviewReport/{reportId}"
                );
            }
        }

        public List<Notification> GetUserNotifications(int userId, bool unreadOnly = false)
        {
            var query = _context.Notifications.Where(n => n.UserId == userId);

            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            return query.OrderByDescending(n => n.CreatedDate).ToList();
        }

        public int GetUnreadCount(int userId)
        {
            return _context.Notifications.Count(n => n.UserId == userId && !n.IsRead);
        }

        public void MarkAsRead(int notificationId)
        {
            var notification = _context.Notifications.Find(notificationId);
            if (notification != null && !notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadDate = DateTime.Now;
                _context.SaveChanges();
            }
        }

        public void MarkAllAsRead(int userId)
        {
            var notifications = _context.Notifications.Where(n => n.UserId == userId && !n.IsRead).ToList();
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadDate = DateTime.Now;
            }
            _context.SaveChanges();
        }

        public void Dispose()
        {
            _context?.Dispose();
        }

        internal void NotifyFileDeleted(int fileId, List<int> usersWithAccess, int userId)
        {
            var file = _context.SharedFiles.Find(fileId);
            var removedBy = _context.Users.Find(userId);

            if (file != null && removedBy != null)
            {
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
                }
            }
        }
    }
}
