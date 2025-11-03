using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileSharePortal.Models
{
    public class ApplicationLogFile
    {
        [Key]
        public int LogFileId { get; set; }

        [Required]
        public int ExecutionId { get; set; }

        [ForeignKey("ExecutionId")]
        public virtual ApplicationExecution Execution { get; set; }

        [Required]
        [StringLength(255)]
        public string FileName { get; set; }

        [Required]
        [StringLength(100)]
        public string ContentType { get; set; }

        [Required]
        public long FileSize { get; set; }

        [Required]
        public byte[] FileContent { get; set; } // Store file in database

        [Required]
        public DateTime UploadedDate { get; set; } = DateTime.Now;

        [StringLength(500)]
        public string Description { get; set; }
    }
}
