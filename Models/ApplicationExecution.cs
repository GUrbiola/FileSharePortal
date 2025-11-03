using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileSharePortal.Models
{
    public class ApplicationExecution
    {
        [Key]
        public int ExecutionId { get; set; }

        [Required]
        public int ApplicationId { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime? EndTime { get; set; }

        public ApplicationStatus Status { get; set; }

        public string ErrorMessage { get; set; }

        public string ExecutionDetails { get; set; }

        public int? RecordsProcessed { get; set; }

        public int? ExecutedByUserId { get; set; }

        // Navigation properties
        [ForeignKey("ApplicationId")]
        public virtual Application Application { get; set; }

        [ForeignKey("ExecutedByUserId")]
        public virtual User ExecutedBy { get; set; }

        public virtual ICollection<ApplicationLogFile> LogFiles { get; set; }

        public ApplicationExecution()
        {
            StartTime = DateTime.Now;
            Status = ApplicationStatus.Running;
            LogFiles = new HashSet<ApplicationLogFile>();
        }
    }
}
