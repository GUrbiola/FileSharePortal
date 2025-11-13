using System;
using System.Linq;
using System.Web;
using FileSharePortal.Data;
using FileSharePortal.Helpers;
using FileSharePortal.Models;
using log4net;

namespace FileSharePortal.Services
{
    public class ThemeService
    {
        private static readonly ILog Logger = LoggingHelper.GetLogger(typeof(ThemeService));
        private readonly FileSharePortalContext _context;

        public ThemeService()
        {
            _context = new FileSharePortalContext();
        }

        public Theme GetActiveTheme()
        {
            Logger.Info("Retrieving active theme");

            try
            {
                var settings = _context.SiteSettings.FirstOrDefault();

                if (settings != null)
                {
                    var theme = _context.Themes.Find(settings.ActiveThemeId);

                    if (theme != null)
                    {
                        Logger.Info($"Using active theme: {theme.ThemeName}");
                        return theme;
                    }
                    else
                    {
                        Logger.Warn($"Active theme not found with ID: {settings.ActiveThemeId}");
                    }
                }
                else
                {
                    Logger.Warn("Site settings not found");
                }

                // Return default theme if no active theme found
                var defaultTheme = _context.Themes.FirstOrDefault(t => t.IsDefault) ?? GetDefaultTheme();
                Logger.Info($"Using default theme: {defaultTheme.ThemeName}");
                return defaultTheme;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, "Error retrieving active theme", ex);
                throw;
            }
        }

        public SiteSettings GetSiteSettings()
        {
            Logger.Info("Retrieving site settings");
            
            try
            {
                var settings = _context.SiteSettings.FirstOrDefault();

                if (settings == null)
                {
                    Logger.Info("Site settings not found, creating default settings");

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

                    Logger.Info($"Default site settings created with ID: {settings.SettingsId}");
                }
                else
                {
                    Logger.Debug($"Site settings found - SiteName: {settings.SiteName}, ActiveThemeId: {settings.ActiveThemeId}");
                }

                return settings;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, "Error retrieving site settings", ex);
                throw;
            }
        }

        public void SetActiveTheme(int themeId)
        {
            Logger.Info($"Setting active theme to: {themeId}");
            
            try
            {
                var settings = GetSiteSettings();
                var theme = _context.Themes.Find(themeId);

                if (theme != null)
                {
                    // Update all themes to inactive
                    var allThemes = _context.Themes.ToList();

                    foreach (var t in allThemes)
                    {
                        if (t.IsActive)
                        {
                            Logger.Debug($"Setting theme '{t.ThemeName}' to inactive");
                            t.IsActive = false;
                        }
                    }

                    // Set new active theme
                    theme.IsActive = true;
                    settings.ActiveThemeId = themeId;

                    _context.SaveChanges();

                    Logger.Info($"Active theme successfully set to: {theme.ThemeName} (ThemeId: {themeId})");
                }
                else
                {
                    Logger.Warn($"Theme not found with ID: {themeId}");
                }
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, $"Error setting active theme to {themeId}", ex);
                throw;
            }
        }

        public string GetThemeCss(Theme theme)
        {
            Logger.Info($"Generating CSS for theme: {theme?.ThemeName ?? "null"}");

            try
            {
                if (theme == null)
                {
                    Logger.Warn("Theme is null, returning empty CSS");
                    return string.Empty;
                }

                var css = $@"
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

                Logger.Debug($"CSS generated successfully for theme: {theme.ThemeName} (length: {css.Length} characters)");
                return css;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, $"Error generating CSS for theme: {theme?.ThemeName}", ex);
                throw;
            }
        }

        private Theme GetDefaultTheme()
        {
            Logger.Info("Creating default theme object");

            try
            {
                var theme = new Theme
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

                Logger.Debug("Default theme object created successfully");
                return theme;
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, "Error creating default theme", ex);
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                _context?.Dispose();
            }
            catch (Exception ex)
            {
                LoggingHelper.LogError(Logger, "Error during dispose", ex);
                throw;
            }
        }
    }
}
