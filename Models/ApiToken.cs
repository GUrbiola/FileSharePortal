using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileSharePortal.Models
{
    public class ApiToken
    {
        [Key]
        public int TokenId { get; set; }

        [Required]
        [StringLength(500)]
        public string Token { get; set; }

        [Required]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        public int? ApplicationId { get; set; }

        [ForeignKey("ApplicationId")]
        public virtual Application Application { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Required]
        public DateTime ExpiresDate { get; set; }

        public bool IsRevoked { get; set; } = false;

        public DateTime? LastUsedDate { get; set; }

        [StringLength(100)]
        public string IpAddress { get; set; }
    }
}
