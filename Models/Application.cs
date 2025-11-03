using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FileSharePortal.Models
{
    public enum ApplicationStatus
    {
        Running,
        Stopped,
        Error,
        Unknown
    }

    public class Application
    {
        [Key]
        public int ApplicationId { get; set; }

        [Required]
        [StringLength(200)]
        public string ApplicationName { get; set; }

        [StringLength(1000)]
        public string Description { get; set; }

        [StringLength(500)]
        public string StatusEndpoint { get; set; }

        [StringLength(500)]
        public string LogPath { get; set; }

        public ApplicationStatus CurrentStatus { get; set; }

        public DateTime? LastStatusCheck { get; set; }

        public DateTime? LastSuccessfulRun { get; set; }

        public bool IsActive { get; set; }

        public int CheckIntervalMinutes { get; set; }

        // API Access fields
        [StringLength(500)]
        public string ApiKey { get; set; }

        public int? RegisteredByUserId { get; set; }

        public DateTime? RegisteredDate { get; set; }

        [StringLength(200)]
        public string ContactEmail { get; set; }

        // Navigation properties
        public virtual ICollection<ApplicationExecution> Executions { get; set; }
        public virtual ICollection<ApiToken> ApiTokens { get; set; }

        public Application()
        {
            Executions = new HashSet<ApplicationExecution>();
            ApiTokens = new HashSet<ApiToken>();
            IsActive = true;
            CheckIntervalMinutes = 5;
            CurrentStatus = ApplicationStatus.Unknown;
        }
    }
}
