using System.ComponentModel.DataAnnotations;

namespace FileSharePortal.ViewModels
{
    public class ThemeViewModel
    {
        public int ThemeId { get; set; }

        [Required(ErrorMessage = "Theme name is required")]
        [StringLength(100)]
        [Display(Name = "Theme Name")]
        public string ThemeName { get; set; }

        [StringLength(500)]
        [Display(Name = "Description")]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Primary Color")]
        [RegularExpression(@"^#([A-Fa-f0-9]{6})$", ErrorMessage = "Invalid color format. Use #RRGGBB")]
        public string PrimaryColor { get; set; }

        [Display(Name = "Primary Hover Color")]
        [RegularExpression(@"^#([A-Fa-f0-9]{6})$", ErrorMessage = "Invalid color format. Use #RRGGBB")]
        public string PrimaryColorHover { get; set; }

        [Display(Name = "Secondary Color")]
        [RegularExpression(@"^#([A-Fa-f0-9]{6})$", ErrorMessage = "Invalid color format. Use #RRGGBB")]
        public string SecondaryColor { get; set; }

        [Display(Name = "Success Color")]
        [RegularExpression(@"^#([A-Fa-f0-9]{6})$", ErrorMessage = "Invalid color format. Use #RRGGBB")]
        public string SuccessColor { get; set; }

        [Display(Name = "Danger Color")]
        [RegularExpression(@"^#([A-Fa-f0-9]{6})$", ErrorMessage = "Invalid color format. Use #RRGGBB")]
        public string DangerColor { get; set; }

        [Display(Name = "Warning Color")]
        [RegularExpression(@"^#([A-Fa-f0-9]{6})$", ErrorMessage = "Invalid color format. Use #RRGGBB")]
        public string WarningColor { get; set; }

        [Display(Name = "Info Color")]
        [RegularExpression(@"^#([A-Fa-f0-9]{6})$", ErrorMessage = "Invalid color format. Use #RRGGBB")]
        public string InfoColor { get; set; }

        [Display(Name = "Light Background")]
        [RegularExpression(@"^#([A-Fa-f0-9]{6})$", ErrorMessage = "Invalid color format. Use #RRGGBB")]
        public string LightBackground { get; set; }

        [Display(Name = "Dark Background")]
        [RegularExpression(@"^#([A-Fa-f0-9]{6})$", ErrorMessage = "Invalid color format. Use #RRGGBB")]
        public string DarkBackground { get; set; }

        [Display(Name = "Sidebar Background")]
        [RegularExpression(@"^#([A-Fa-f0-9]{6})$", ErrorMessage = "Invalid color format. Use #RRGGBB")]
        public string SidebarBackground { get; set; }

        [Display(Name = "Sidebar Text Color")]
        [RegularExpression(@"^#([A-Fa-f0-9]{6})$", ErrorMessage = "Invalid color format. Use #RRGGBB")]
        public string SidebarTextColor { get; set; }

        [Display(Name = "Sidebar Hover Background")]
        [RegularExpression(@"^#([A-Fa-f0-9]{6})$", ErrorMessage = "Invalid color format. Use #RRGGBB")]
        public string SidebarHoverBackground { get; set; }

        [Display(Name = "Sidebar Active Background")]
        [RegularExpression(@"^#([A-Fa-f0-9]{6})$", ErrorMessage = "Invalid color format. Use #RRGGBB")]
        public string SidebarActiveBackground { get; set; }

        public bool IsDefault { get; set; }
    }
}
