using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace FileSharePortal.Models
{
    public class SiteSettings
    {
        [Key]
        public int SettingsId { get; set; }
        [Required]
        public string SiteName { get; set; }
        [Required]
        public string SiteDescription { get; set; }
        public string FaviconPath { get; set; }
        public string LogoPath { get; set; }
        public int LastModifiedByUserId { get; set; }
        public DateTime LastModifiedDate { get; set; }
        [Required]
        [DefaultValue(1)]
        public int ActiveThemeId { get; set; }
    }
}
