using System.ComponentModel.DataAnnotations;
using System.Web;

namespace FileSharePortal.ViewModels
{
    public class SiteSettingsViewModel
    {
        public int SettingsId { get; set; }

        [Required(ErrorMessage = "Site name is required")]
        [StringLength(200)]
        [Display(Name = "Site Name")]
        public string SiteName { get; set; }

        [StringLength(500)]
        [Display(Name = "Site Description")]
        public string SiteDescription { get; set; }

        [Display(Name = "Logo Image")]
        public HttpPostedFileBase LogoFile { get; set; }

        public string CurrentLogoPath { get; set; }

        [Display(Name = "Favicon Image")]
        public HttpPostedFileBase FaviconFile { get; set; }

        public string CurrentFaviconPath { get; set; }

        public int? ActiveThemeId { get; set; }
    }
}
