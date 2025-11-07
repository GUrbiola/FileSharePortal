using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileSharePortal.Models
{
    public class ErrorLog
    {
        [Key]
        public int ErrorLogId { get; set; }

        [Required]
        public string ErrorMessage { get; set; }

        public string InnerException { get; set; }

        public string StackTrace { get; set; }

        [Required]
        public DateTime OccurredAt { get; set; }

        [StringLength(500)]
        public string RequestUrl { get; set; }

        [StringLength(50)]
        public string HttpMethod { get; set; }

        [StringLength(200)]
        public string UserAgent { get; set; }

        [StringLength(100)]
        public string IpAddress { get; set; }

        public int? UserId { get; set; }

        [StringLength(200)]
        public string Username { get; set; }

        [StringLength(100)]
        public string ControllerName { get; set; }

        [StringLength(100)]
        public string ActionName { get; set; }

        [StringLength(500)]
        public string ExceptionType { get; set; }

        [StringLength(500)]
        public string SourceFile { get; set; }

        public int? LineNumber { get; set; }

        public bool IsResolved { get; set; }

        public DateTime? ResolvedAt { get; set; }

        public int? ResolvedByUserId { get; set; }

        [StringLength(1000)]
        public string ResolutionNotes { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        public ErrorLog()
        {
            OccurredAt = DateTime.Now;
            IsResolved = false;
        }
    }
}
