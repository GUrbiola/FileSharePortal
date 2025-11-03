using System.Linq;
using System.Web;
using FileSharePortal.Data;
using FileSharePortal.Models;

namespace FileSharePortal.Services
{
    public class ThemeService
    {
        private readonly FileSharePortalContext _context;

        public ThemeService()
        {
            _context = new FileSharePortalContext();
        }

        public Theme GetActiveTheme()
        {
            var settings = _context.SiteSettings.FirstOrDefault();
            if (settings != null)
            {
                var theme = _context.Themes.Find(settings.ActiveThemeId);
                if (theme != null)
                {
                    return theme;
                }
            }

            // Return default theme if no active theme found
            return _context.Themes.FirstOrDefault(t => t.IsDefault) ?? GetDefaultTheme();
        }

        public SiteSettings GetSiteSettings()
        {
            var settings = _context.SiteSettings.FirstOrDefault();
            if (settings == null)
            {
                settings = new SiteSettings
                {
                    SiteName = "File Share Portal",
                    SiteDescription = "Secure file sharing platform",
                    ActiveThemeId = 1,
                    FaviconPath = "/Images/ff-favicon.ico",
                    LogoPath = "/Images/ff-logo.png",
                    LastModifiedDate = System.DateTime.Now
                };
                _context.SiteSettings.Add(settings);
                _context.SaveChanges();
            }
            return settings;
        }

        public void SetActiveTheme(int themeId)
        {
            var settings = GetSiteSettings();
            var theme = _context.Themes.Find(themeId);

            if (theme != null)
            {
                // Update all themes to inactive
                var allThemes = _context.Themes.ToList();
                foreach (var t in allThemes)
                {
                    t.IsActive = false;
                }

                // Set new active theme
                theme.IsActive = true;
                settings.ActiveThemeId = themeId;
                _context.SaveChanges();
            }
        }

        public string GetThemeCss(Theme theme)
        {
            if (theme == null)
            {
                return string.Empty;
            }

            return $@"
                :root {{
                    --primary-color: {theme.PrimaryColor};
                    --primary-color-hover: {theme.PrimaryColorHover ?? theme.PrimaryColor};
                    --secondary-color: {theme.SecondaryColor ?? "#6c757d"};
                    --success-color: {theme.SuccessColor ?? "#198754"};
                    --danger-color: {theme.DangerColor ?? "#dc3545"};
                    --warning-color: {theme.WarningColor ?? "#ffc107"};
                    --info-color: {theme.InfoColor ?? "#0dcaf0"};
                    --light-bg: {theme.LightBackground ?? "#f8f9fa"};
                    --dark-bg: {theme.DarkBackground ?? "#212529"};
                    --sidebar-bg: {theme.SidebarBackground ?? "#ffffff"};
                    --sidebar-text: {theme.SidebarTextColor ?? "#495057"};
                    --sidebar-hover-bg: {theme.SidebarHoverBackground ?? "#f8f9fa"};
                    --sidebar-active-bg: {theme.SidebarActiveBackground ?? "#e7f3ff"};
                }}

                body {{
                    background-color: {theme.LightBackground ?? "#f5f5f5"};
                }}

                .top-header {{
                    background-color: {theme.PrimaryColor} !important;
                }}

                .btn-primary {{
                    background-color: {theme.PrimaryColor} !important;
                    border-color: {theme.PrimaryColor} !important;
                }}

                .btn-primary:hover {{
                    background-color: {theme.PrimaryColorHover ?? theme.PrimaryColor} !important;
                    border-color: {theme.PrimaryColorHover ?? theme.PrimaryColor} !important;
                }}

                .sidebar {{
                    background-color: {theme.SidebarBackground ?? "#ffffff"};
                }}

                .sidebar-link {{
                    color: {theme.SidebarTextColor ?? "#495057"};
                }}

                .sidebar-link:hover {{
                    background-color: {theme.SidebarHoverBackground ?? "#f8f9fa"};
                }}

                .sidebar-link.active {{
                    background-color: {theme.SidebarActiveBackground ?? "#e7f3ff"};
                    color: {theme.PrimaryColor};
                    border-left-color: {theme.PrimaryColor};
                }}

                .stat-card {{
                    background: linear-gradient(135deg, {theme.PrimaryColor} 0%, {theme.PrimaryColorHover ?? theme.PrimaryColor} 100%);
                }}

                .card-header {{
                    border-bottom-color: {theme.PrimaryColor};
                }}
            ";
        }

        private Theme GetDefaultTheme()
        {
            return new Theme
            {
                ThemeName = "Default",
                PrimaryColor = "#0d6efd",
                PrimaryColorHover = "#0b5ed7",
                SecondaryColor = "#6c757d",
                SuccessColor = "#198754",
                DangerColor = "#dc3545",
                WarningColor = "#ffc107",
                InfoColor = "#0dcaf0",
                LightBackground = "#f8f9fa",
                DarkBackground = "#212529",
                SidebarBackground = "#ffffff",
                SidebarTextColor = "#495057",
                SidebarHoverBackground = "#f8f9fa",
                SidebarActiveBackground = "#e7f3ff"
            };
        }

        public void Dispose()
        {
            _context?.Dispose();
        }
    }
}
