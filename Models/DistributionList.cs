using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace FileSharePortal.Models
{
    public class DistributionList
    {
        [Key]
        public int DistributionListId { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [Required]
        [StringLength(500)]
        public string ADDistinguishedName { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? LastSyncDate { get; set; }

        // Navigation properties
        public virtual ICollection<RoleDistributionList> RoleDistributionLists { get; set; }

        public DistributionList()
        {
            RoleDistributionLists = new HashSet<RoleDistributionList>();
            CreatedDate = DateTime.Now;
        }
    }
}
