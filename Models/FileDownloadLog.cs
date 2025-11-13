using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileSharePortal.Models
{
    public class FileDownloadLog
    {
        [Key]
        public int DownloadLogId { get; set; }

        [Required]
        public int FileId { get; set; }

        [Required]
        public int DownloadedByUserId { get; set; }

        [Required]
        public DateTime DownloadedDate { get; set; }

        [StringLength(45)]
        public string IpAddress { get; set; }

        [StringLength(500)]
        public string UserAgent { get; set; }

        // Navigation properties
        [ForeignKey("FileId")]
        public virtual SharedFile SharedFile { get; set; }

        [ForeignKey("DownloadedByUserId")]
        public virtual User DownloadedBy { get; set; }

        public FileDownloadLog()
        {
            DownloadedDate = DateTime.Now;
        }
    }
}
