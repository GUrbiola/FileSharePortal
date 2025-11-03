using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileSharePortal.Models
{
    public enum ReportStatus
    {
        Pending,
        UnderReview,
        Resolved,
        Dismissed
    }

    public class FileReport
    {
        [Key]
        public int ReportId { get; set; }

        [Required]
        public int FileId { get; set; }

        [Required]
        public int ReportedByUserId { get; set; }

        [Required]
        [StringLength(200)]
        public string Reason { get; set; }

        [Required]
        public string Details { get; set; }

        public DateTime ReportedDate { get; set; }

        public ReportStatus Status { get; set; }

        public int? ReviewedByUserId { get; set; }

        public DateTime? ReviewedDate { get; set; }

        [StringLength(1000)]
        public string AdminNotes { get; set; }

        // Navigation properties
        [ForeignKey("FileId")]
        public virtual SharedFile SharedFile { get; set; }

        [ForeignKey("ReportedByUserId")]
        public virtual User ReportedBy { get; set; }

        public FileReport()
        {
            ReportedDate = DateTime.Now;
            Status = ReportStatus.Pending;
        }
    }
}
