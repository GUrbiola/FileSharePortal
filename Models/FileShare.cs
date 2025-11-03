using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileSharePortal.Models
{
    public class FileShare
    {
        [Key]
        public int FileShareId { get; set; }

        [Required]
        public int FileId { get; set; }

        public int? SharedWithUserId { get; set; }

        public int? SharedWithRoleId { get; set; }

        public DateTime SharedDate { get; set; }

        [Required]
        public int SharedByUserId { get; set; }

        public bool CanDownload { get; set; }

        public DateTime? ExpirationDate { get; set; }

        // Navigation properties
        [ForeignKey("FileId")]
        public virtual SharedFile SharedFile { get; set; }

        [ForeignKey("SharedWithUserId")]
        public virtual User SharedWithUser { get; set; }

        [ForeignKey("SharedWithRoleId")]
        public virtual Role SharedWithRole { get; set; }

        public FileShare()
        {
            SharedDate = DateTime.Now;
            CanDownload = true;
        }
    }
}
