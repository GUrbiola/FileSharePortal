using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileSharePortal.Models
{
    public class RoleDistributionList
    {
        [Key]
        public int RoleDistributionListId { get; set; }

        [Required]
        public int RoleId { get; set; }

        [Required]
        public int DistributionListId { get; set; }

        public DateTime AddedDate { get; set; }

        // Navigation properties
        [ForeignKey("RoleId")]
        public virtual Role Role { get; set; }

        [ForeignKey("DistributionListId")]
        public virtual DistributionList DistributionList { get; set; }

        public RoleDistributionList()
        {
            AddedDate = DateTime.Now;
        }
    }
}
