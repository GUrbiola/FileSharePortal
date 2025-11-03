using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileSharePortal.Models
{
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        [Index(IsUnique = true)]
        public string Username { get; set; }

        [StringLength(256)]
        public string PasswordHash { get; set; }

        [Required]
        [StringLength(200)]
        public string FullName { get; set; }

        [Required]
        [StringLength(200)]
        [EmailAddress]
        public string Email { get; set; }

        public bool IsAdmin { get; set; }

        public bool IsActive { get; set; }

        public bool IsFromActiveDirectory { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? LastLoginDate { get; set; }

        // Navigation properties
        public virtual ICollection<SharedFile> UploadedFiles { get; set; }
        public virtual ICollection<RoleUser> RoleUsers { get; set; }
        public virtual ICollection<Notification> Notifications { get; set; }
        public virtual ICollection<FileReport> FileReports { get; set; }

        public User()
        {
            UploadedFiles = new HashSet<SharedFile>();
            RoleUsers = new HashSet<RoleUser>();
            Notifications = new HashSet<Notification>();
            FileReports = new HashSet<FileReport>();
            CreatedDate = DateTime.Now;
            IsActive = true;
        }
    }
}
