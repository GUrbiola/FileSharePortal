using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FileSharePortal.Models
{
    public class Role
    {
        [Key]
        public int RoleId { get; set; }

        [Required]
        [StringLength(100)]
        public string RoleName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public DateTime CreatedDate { get; set; }

        public int CreatedByUserId { get; set; }

        // Navigation properties
        public virtual ICollection<RoleUser> RoleUsers { get; set; }
        public virtual ICollection<RoleDistributionList> RoleDistributionLists { get; set; }
        public virtual ICollection<FileShare> FileShares { get; set; }

        public Role()
        {
            RoleUsers = new HashSet<RoleUser>();
            RoleDistributionLists = new HashSet<RoleDistributionList>();
            FileShares = new HashSet<FileShare>();
            CreatedDate = DateTime.Now;
        }
    }
}
