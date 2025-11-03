using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileSharePortal.Models
{
    public class RoleUser
    {
        [Key]
        public int RoleUserId { get; set; }

        [Required]
        public int RoleId { get; set; }

        [Required]
        public int UserId { get; set; }

        public DateTime AddedDate { get; set; }

        // Navigation properties
        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; }

        [ForeignKey("UserId")]
        public virtual User User { get; set; }

        public RoleUser()
        {
            AddedDate = DateTime.Now;
        }
    }
}
