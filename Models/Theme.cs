using System.ComponentModel.DataAnnotations;

namespace FileSharePortal.Models
{
    public class Theme
    {
        [Key]
        public int ThemeId { get; set; }
        [Required]
        [StringLength(50)]
        public string ThemeName { get; set; }
        [Required]
        [StringLength(200)]
        public string Description { get; set; }
        [Required]
        [StringLength(10)]
        public string PrimaryColor { get; set; }
        [Required]
        [StringLength(10)]
        public string PrimaryColorHover { get; set; }
        [Required]
        [StringLength(10)]
        public string SecondaryColor { get; set; }
        [Required]
        [StringLength(10)]
        public string SuccessColor { get; set; }
        [Required]
        [StringLength(10)]
        public string DangerColor { get; set; }
        [Required]
        [StringLength(10)]
        public string WarningColor { get; set; }
        [Required]
        [StringLength(10)]
        public string InfoColor { get; set; }
        [Required]
        [StringLength(10)]
        public string LightBackground { get; set; }
        [Required]
        [StringLength(10)]
        public string DarkBackground { get; set; }
        [Required]
        [StringLength(10)]
        public string SidebarBackground { get; set; }
        [Required]
        [StringLength(10)]
        public string SidebarTextColor { get; set; }
        [Required]
        [StringLength(10)]
        public string SidebarHoverBackground { get; set; }
        [Required]
        [StringLength(10)]
        public string SidebarActiveBackground { get; set; }
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }
        public int CreatedByUserId { get; set; }
    }
}
