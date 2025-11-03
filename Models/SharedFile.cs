using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileSharePortal.Models
{
    public class SharedFile
    {
        [Key]
        public int FileId { get; set; }

        [Required]
        [StringLength(500)]
        public string FileName { get; set; }

        [Required]
        [StringLength(1000)]
        public string FilePath { get; set; }

        [Required]
        [StringLength(200)]
        public string ContentType { get; set; }

        public long FileSize { get; set; }

        [Required]
        public int UploadedByUserId { get; set; }

        public DateTime UploadedDate { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime? DeletedDate { get; set; }

        public int? DeletedByUserId { get; set; }

        public int DownloadCount { get; set; }

        // Navigation properties
        [ForeignKey("UploadedByUserId")]
        public virtual User UploadedBy { get; set; }

        public virtual ICollection<FileShare> FileShares { get; set; }
        public virtual ICollection<FileReport> FileReports { get; set; }

        public SharedFile()
        {
            FileShares = new HashSet<FileShare>();
            FileReports = new HashSet<FileReport>();
            UploadedDate = DateTime.Now;
            IsDeleted = false;
            DownloadCount = 0;
        }
    }
}
