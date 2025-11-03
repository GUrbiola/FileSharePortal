using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileSharePortal.Models
{
    public enum NotificationType
    {
        FileShared,
        FileReported,
        FileDeleted,
        RoleAssigned,
        General,
        ApplicationAlert
    }

    public class Notification
    {
        [Key]
        public int NotificationId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [StringLength(500)]
        public string Title { get; set; }

        [Required]
        public string Message { get; set; }

        public NotificationType Type { get; set; }

        public int? RelatedFileId { get; set; }

        public int? RelatedApplicationId { get; set; }

        public DateTime CreatedDate { get; set; }

        public bool IsRead { get; set; }

        public DateTime? ReadDate { get; set; }

        [StringLength(500)]
        public string ActionUrl { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        public Notification()
        {
            CreatedDate = DateTime.Now;
            IsRead = false;
        }
    }
}
