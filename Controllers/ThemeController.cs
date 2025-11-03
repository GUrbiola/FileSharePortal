using System;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using FileSharePortal.Data;
using FileSharePortal.Models;
using FileSharePortal.Services;
using FileSharePortal.ViewModels;

namespace FileSharePortal.Controllers
{
    [Authorize]
    public class ThemeController : Controller
    {
        private readonly FileSharePortalContext _context;
        private readonly AuthenticationService _authService;
        private readonly ThemeService _themeService;

        public ThemeController()
        {
            _context = new FileSharePortalContext();
            _authService = new AuthenticationService();
            _themeService = new ThemeService();
        }

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null || !currentUser.IsAdmin)
            {
                filterContext.Result = new RedirectToRouteResult(
                    new System.Web.Routing.RouteValueDictionary
                    {
                        { "controller", "Account" },
                        { "action", "AccessDenied" }
                    });
            }
            base.OnActionExecuting(filterContext);
        }

        // GET: Theme
        public ActionResult Index()
        {
            var currentUser = _authService.GetCurrentUser();
            ViewBag.CurrentUser = currentUser;

            var themes = _context.Themes.OrderBy(t => t.ThemeName).ToList();
            var activeTheme = _themeService.GetActiveTheme();
            ViewBag.ActiveThemeId = activeTheme?.ThemeId;

            return View(themes);
        }

        // GET: Theme/Create
        public ActionResult Create()
        {
            var currentUser = _authService.GetCurrentUser();
            ViewBag.CurrentUser = currentUser;

            var viewModel = new ThemeViewModel
            {
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

            return View(viewModel);
        }

        // POST: Theme/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(ThemeViewModel model)
        {
            var currentUser = _authService.GetCurrentUser();

            if (ModelState.IsValid)
            {
                var theme = new Theme
                {
                    ThemeName = model.ThemeName,
                    Description = model.Description,
                    PrimaryColor = model.PrimaryColor,
                    PrimaryColorHover = model.PrimaryColorHover ?? model.PrimaryColor,
                    SecondaryColor = model.SecondaryColor ?? "#6c757d",
                    SuccessColor = model.SuccessColor ?? "#198754",
                    DangerColor = model.DangerColor ?? "#dc3545",
                    WarningColor = model.WarningColor ?? "#ffc107",
                    InfoColor = model.InfoColor ?? "#0dcaf0",
                    LightBackground = model.LightBackground ?? "#f8f9fa",
                    DarkBackground = model.DarkBackground ?? "#212529",
                    SidebarBackground = model.SidebarBackground ?? "#ffffff",
                    SidebarTextColor = model.SidebarTextColor ?? "#495057",
                    SidebarHoverBackground = model.SidebarHoverBackground ?? "#f8f9fa",
                    SidebarActiveBackground = model.SidebarActiveBackground ?? "#e7f3ff",
                    IsDefault = model.IsDefault,
                    CreatedByUserId = currentUser.UserId
                };

                _context.Themes.Add(theme);
                _context.SaveChanges();

                TempData["Success"] = "Theme created successfully!";
                return RedirectToAction("Index");
            }

            ViewBag.CurrentUser = currentUser;
            return View(model);
        }

        // GET: Theme/Edit/5
        public ActionResult Edit(int id)
        {
            var currentUser = _authService.GetCurrentUser();
            ViewBag.CurrentUser = currentUser;

            var theme = _context.Themes.Find(id);
            if (theme == null)
            {
                return HttpNotFound();
            }

            var viewModel = new ThemeViewModel
            {
                ThemeId = theme.ThemeId,
                ThemeName = theme.ThemeName,
                Description = theme.Description,
                PrimaryColor = theme.PrimaryColor,
                PrimaryColorHover = theme.PrimaryColorHover,
                SecondaryColor = theme.SecondaryColor,
                SuccessColor = theme.SuccessColor,
                DangerColor = theme.DangerColor,
                WarningColor = theme.WarningColor,
                InfoColor = theme.InfoColor,
                LightBackground = theme.LightBackground,
                DarkBackground = theme.DarkBackground,
                SidebarBackground = theme.SidebarBackground,
                SidebarTextColor = theme.SidebarTextColor,
                SidebarHoverBackground = theme.SidebarHoverBackground,
                SidebarActiveBackground = theme.SidebarActiveBackground,
                IsDefault = theme.IsDefault
            };

            return View(viewModel);
        }

        // POST: Theme/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(ThemeViewModel model)
        {
            var currentUser = _authService.GetCurrentUser();

            if (ModelState.IsValid)
            {
                var theme = _context.Themes.Find(model.ThemeId);
                if (theme == null)
                {
                    return HttpNotFound();
                }

                theme.ThemeName = model.ThemeName;
                theme.Description = model.Description;
                theme.PrimaryColor = model.PrimaryColor;
                theme.PrimaryColorHover = model.PrimaryColorHover;
                theme.SecondaryColor = model.SecondaryColor;
                theme.SuccessColor = model.SuccessColor;
                theme.DangerColor = model.DangerColor;
                theme.WarningColor = model.WarningColor;
                theme.InfoColor = model.InfoColor;
                theme.LightBackground = model.LightBackground;
                theme.DarkBackground = model.DarkBackground;
                theme.SidebarBackground = model.SidebarBackground;
                theme.SidebarTextColor = model.SidebarTextColor;
                theme.SidebarHoverBackground = model.SidebarHoverBackground;
                theme.SidebarActiveBackground = model.SidebarActiveBackground;
                theme.IsDefault = model.IsDefault;

                _context.SaveChanges();

                TempData["Success"] = "Theme updated successfully!";
                return RedirectToAction("Index");
            }

            ViewBag.CurrentUser = currentUser;
            return View(model);
        }

        // POST: Theme/Activate/5
        [HttpPost]
        public JsonResult Activate(int id)
        {
            try
            {
                _themeService.SetActiveTheme(id);
                return Json(new { success = true, message = "Theme activated successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Theme/Delete/5
        [HttpPost]
        public JsonResult Delete(int id)
        {
            try
            {
                var theme = _context.Themes.Find(id);
                if (theme == null)
                {
                    return Json(new { success = false, message = "Theme not found" });
                }

                if (theme.IsActive)
                {
                    return Json(new { success = false, message = "Cannot delete active theme" });
                }

                if (theme.IsDefault)
                {
                    return Json(new { success = false, message = "Cannot delete default theme" });
                }

                _context.Themes.Remove(theme);
                _context.SaveChanges();

                return Json(new { success = true, message = "Theme deleted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Theme/Copy/5
        [HttpPost]
        public JsonResult Copy(int id, string newName)
        {
            try
            {
                var currentUser = _authService.GetCurrentUser();
                var sourceTheme = _context.Themes.Find(id);

                if (sourceTheme == null)
                {
                    return Json(new { success = false, message = "Source theme not found" });
                }

                if (string.IsNullOrWhiteSpace(newName))
                {
                    return Json(new { success = false, message = "Theme name cannot be empty" });
                }

                // Check if name already exists
                if (_context.Themes.Any(t => t.ThemeName == newName))
                {
                    return Json(new { success = false, message = "A theme with this name already exists" });
                }

                // Create copy
                var newTheme = new Theme
                {
                    ThemeName = newName,
                    Description = sourceTheme.Description,
                    PrimaryColor = sourceTheme.PrimaryColor,
                    PrimaryColorHover = sourceTheme.PrimaryColorHover,
                    SecondaryColor = sourceTheme.SecondaryColor,
                    SuccessColor = sourceTheme.SuccessColor,
                    DangerColor = sourceTheme.DangerColor,
                    WarningColor = sourceTheme.WarningColor,
                    InfoColor = sourceTheme.InfoColor,
                    LightBackground = sourceTheme.LightBackground,
                    DarkBackground = sourceTheme.DarkBackground,
                    SidebarBackground = sourceTheme.SidebarBackground,
                    SidebarTextColor = sourceTheme.SidebarTextColor,
                    SidebarHoverBackground = sourceTheme.SidebarHoverBackground,
                    SidebarActiveBackground = sourceTheme.SidebarActiveBackground,
                    IsDefault = false,
                    IsActive = false,
                    CreatedByUserId = currentUser.UserId
                };

                _context.Themes.Add(newTheme);
                _context.SaveChanges();

                return Json(new { success = true, message = $"Theme '{newName}' created successfully", themeId = newTheme.ThemeId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: Theme/RestoreDefaults
        [HttpPost]
        public JsonResult RestoreDefaults()
        {
            try
            {
                var currentUser = _authService.GetCurrentUser();
                int restoredCount = 0;
                int createdCount = 0;

                // Define all default themes
                var defaultThemes = new[]
                {
                    new { Name = "Default Blue", Description = "Classic blue theme with professional appearance", Primary = "#0d6efd", PrimaryHover = "#0b5ed7", Secondary = "#6c757d", Success = "#198754", Danger = "#dc3545", Warning = "#ffc107", Info = "#0dcaf0", Light = "#f8f9fa", Dark = "#212529", SidebarBg = "#ffffff", SidebarText = "#495057", SidebarHover = "#f8f9fa", SidebarActive = "#e7f3ff" },
                    new { Name = "Dark Mode", Description = "Modern dark theme for reduced eye strain", Primary = "#375a7f", PrimaryHover = "#2c4866", Secondary = "#444444", Success = "#00bc8c", Danger = "#e74c3c", Warning = "#f39c12", Info = "#3498db", Light = "#343a40", Dark = "#1a1a1a", SidebarBg = "#222222", SidebarText = "#adb5bd", SidebarHover = "#2c3034", SidebarActive = "#375a7f" },
                    new { Name = "Forest Green", Description = "Nature-inspired green theme", Primary = "#2d7a3e", PrimaryHover = "#245c30", Secondary = "#6c757d", Success = "#28a745", Danger = "#dc3545", Warning = "#ffc107", Info = "#17a2b8", Light = "#f0fff0", Dark = "#1a3d1f", SidebarBg = "#f8fff8", SidebarText = "#2d5c36", SidebarHover = "#e8f5e9", SidebarActive = "#c8e6c9" },
                    new { Name = "Royal Purple", Description = "Elegant purple theme with royal touch", Primary = "#6f42c1", PrimaryHover = "#5a32a3", Secondary = "#6c757d", Success = "#198754", Danger = "#dc3545", Warning = "#ffc107", Info = "#0dcaf0", Light = "#f5f0ff", Dark = "#2a1a40", SidebarBg = "#faf8fc", SidebarText = "#4a2c70", SidebarHover = "#f3e5f5", SidebarActive = "#e1bee7" },
                    new { Name = "Ocean Blue", Description = "Deep ocean blue with calming effect", Primary = "#006ba6", PrimaryHover = "#004d75", Secondary = "#6c757d", Success = "#06d6a0", Danger = "#ef476f", Warning = "#ffd166", Info = "#118ab2", Light = "#e0f2fe", Dark = "#001f3f", SidebarBg = "#e8f4f8", SidebarText = "#003d5b", SidebarHover = "#d4e9f0", SidebarActive = "#b3dae9" },
                    new { Name = "Sunset Orange", Description = "Warm sunset orange theme", Primary = "#ff6b35", PrimaryHover = "#e85a2a", Secondary = "#6c757d", Success = "#4ecdc4", Danger = "#c44536", Warning = "#f7b731", Info = "#5f27cd", Light = "#fff5f0", Dark = "#4a1f0f", SidebarBg = "#fff8f0", SidebarText = "#cc4418", SidebarHover = "#ffe8d6", SidebarActive = "#ffd4b3" },
                    new { Name = "Slate Gray", Description = "Professional gray theme for corporate environment", Primary = "#475569", PrimaryHover = "#334155", Secondary = "#64748b", Success = "#22c55e", Danger = "#ef4444", Warning = "#f59e0b", Info = "#3b82f6", Light = "#f1f5f9", Dark = "#1e293b", SidebarBg = "#f8fafc", SidebarText = "#1e293b", SidebarHover = "#e2e8f0", SidebarActive = "#cbd5e1" },
                    new { Name = "Ruby Red", Description = "Bold red theme with strong presence", Primary = "#b91c1c", PrimaryHover = "#991b1b", Secondary = "#6c757d", Success = "#059669", Danger = "#dc2626", Warning = "#f59e0b", Info = "#06b6d4", Light = "#fef2f2", Dark = "#450a0a", SidebarBg = "#fef2f2", SidebarText = "#7f1d1d", SidebarHover = "#fee2e2", SidebarActive = "#fecaca" },
                    new { Name = "Mint Fresh", Description = "Fresh mint green theme for modern look", Primary = "#10b981", PrimaryHover = "#059669", Secondary = "#6c757d", Success = "#14b8a6", Danger = "#f43f5e", Warning = "#fb923c", Info = "#06b6d4", Light = "#ecfdf5", Dark = "#022c22", SidebarBg = "#f0fdf4", SidebarText = "#065f46", SidebarHover = "#dcfce7", SidebarActive = "#bbf7d0" },
                    new { Name = "Corporate Blue", Description = "Traditional corporate blue theme", Primary = "#1e40af", PrimaryHover = "#1e3a8a", Secondary = "#6c757d", Success = "#16a34a", Danger = "#dc2626", Warning = "#ea580c", Info = "#0284c7", Light = "#eff6ff", Dark = "#172554", SidebarBg = "#eff6ff", SidebarText = "#1e3a8a", SidebarHover = "#dbeafe", SidebarActive = "#bfdbfe" },
                    new { Name = "Midnight Black", Description = "Ultra-dark theme for night work", Primary = "#1f2937", PrimaryHover = "#111827", Secondary = "#4b5563", Success = "#10b981", Danger = "#ef4444", Warning = "#f59e0b", Info = "#3b82f6", Light = "#374151", Dark = "#030712", SidebarBg = "#0f172a", SidebarText = "#d1d5db", SidebarHover = "#1e293b", SidebarActive = "#334155" },
                    new { Name = "Lavender Dream", Description = "Soft lavender theme with gentle colors", Primary = "#8b5cf6", PrimaryHover = "#7c3aed", Secondary = "#6c757d", Success = "#10b981", Danger = "#f43f5e", Warning = "#fb923c", Info = "#06b6d4", Light = "#faf5ff", Dark = "#3b0764", SidebarBg = "#faf5ff", SidebarText = "#5b21b6", SidebarHover = "#f3e8ff", SidebarActive = "#e9d5ff" }
                };

                foreach (var defaultTheme in defaultThemes)
                {
                    var existingTheme = _context.Themes.FirstOrDefault(t => t.ThemeName == defaultTheme.Name);

                    if (existingTheme != null)
                    {
                        // Restore existing theme
                        existingTheme.Description = defaultTheme.Description;
                        existingTheme.PrimaryColor = defaultTheme.Primary;
                        existingTheme.PrimaryColorHover = defaultTheme.PrimaryHover;
                        existingTheme.SecondaryColor = defaultTheme.Secondary;
                        existingTheme.SuccessColor = defaultTheme.Success;
                        existingTheme.DangerColor = defaultTheme.Danger;
                        existingTheme.WarningColor = defaultTheme.Warning;
                        existingTheme.InfoColor = defaultTheme.Info;
                        existingTheme.LightBackground = defaultTheme.Light;
                        existingTheme.DarkBackground = defaultTheme.Dark;
                        existingTheme.SidebarBackground = defaultTheme.SidebarBg;
                        existingTheme.SidebarTextColor = defaultTheme.SidebarText;
                        existingTheme.SidebarHoverBackground = defaultTheme.SidebarHover;
                        existingTheme.SidebarActiveBackground = defaultTheme.SidebarActive;
                        restoredCount++;
                    }
                    else
                    {
                        // Create missing default theme
                        var newTheme = new Theme
                        {
                            ThemeName = defaultTheme.Name,
                            Description = defaultTheme.Description,
                            PrimaryColor = defaultTheme.Primary,
                            PrimaryColorHover = defaultTheme.PrimaryHover,
                            SecondaryColor = defaultTheme.Secondary,
                            SuccessColor = defaultTheme.Success,
                            DangerColor = defaultTheme.Danger,
                            WarningColor = defaultTheme.Warning,
                            InfoColor = defaultTheme.Info,
                            LightBackground = defaultTheme.Light,
                            DarkBackground = defaultTheme.Dark,
                            SidebarBackground = defaultTheme.SidebarBg,
                            SidebarTextColor = defaultTheme.SidebarText,
                            SidebarHoverBackground = defaultTheme.SidebarHover,
                            SidebarActiveBackground = defaultTheme.SidebarActive,
                            IsDefault = true,
                            IsActive = defaultTheme.Name == "Default Blue" && !_context.Themes.Any(t => t.IsActive),
                            CreatedByUserId = currentUser.UserId
                        };
                        _context.Themes.Add(newTheme);
                        createdCount++;
                    }
                }

                _context.SaveChanges();

                return Json(new
                {
                    success = true,
                    message = $"Restored {restoredCount} themes and created {createdCount} missing default themes"
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // GET: Theme/SiteSettings
        public ActionResult SiteSettings()
        {
            var currentUser = _authService.GetCurrentUser();
            ViewBag.CurrentUser = currentUser;

            var settings = _themeService.GetSiteSettings();
            var viewModel = new SiteSettingsViewModel
            {
                SettingsId = settings.SettingsId,
                SiteName = settings.SiteName,
                SiteDescription = settings.SiteDescription,
                CurrentLogoPath = settings.LogoPath,
                CurrentFaviconPath = settings.FaviconPath,
                ActiveThemeId = settings.ActiveThemeId
            };

            ViewBag.Themes = _context.Themes.ToList();

            return View(viewModel);
        }

        // POST: Theme/SiteSettings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SiteSettings(SiteSettingsViewModel model)
        {
            var currentUser = _authService.GetCurrentUser();

            if (ModelState.IsValid)
            {
                var settings = _themeService.GetSiteSettings();

                settings.SiteName = model.SiteName;
                settings.SiteDescription = model.SiteDescription;
                settings.ActiveThemeId = model.ActiveThemeId.HasValue ? model.ActiveThemeId.Value : 1;
                settings.LastModifiedByUserId = currentUser.UserId;
                settings.LastModifiedDate = DateTime.Now;

                // Handle logo upload
                if (model.LogoFile != null && model.LogoFile.ContentLength > 0)
                {
                    var logoFileName = $"logo_{DateTime.Now.Ticks}{Path.GetExtension(model.LogoFile.FileName)}";
                    var logoPath = Path.Combine(Server.MapPath("~/Content/images"), logoFileName);

                    var directory = Path.GetDirectoryName(logoPath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    model.LogoFile.SaveAs(logoPath);
                    settings.LogoPath = $"/Content/images/{logoFileName}";
                }

                // Handle favicon upload
                if (model.FaviconFile != null && model.FaviconFile.ContentLength > 0)
                {
                    var faviconFileName = $"favicon_{DateTime.Now.Ticks}{Path.GetExtension(model.FaviconFile.FileName)}";
                    var faviconPath = Path.Combine(Server.MapPath("~/Content/images"), faviconFileName);

                    var directory = Path.GetDirectoryName(faviconPath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    model.FaviconFile.SaveAs(faviconPath);
                    settings.FaviconPath = $"/Content/images/{faviconFileName}";
                }

                // Activate theme if changed
                if (model.ActiveThemeId.HasValue)
                {
                    _themeService.SetActiveTheme(model.ActiveThemeId.Value);
                }

                _context.SaveChanges();

                TempData["Success"] = "Site settings updated successfully!";
                return RedirectToAction("SiteSettings");
            }

            ViewBag.CurrentUser = currentUser;
            ViewBag.Themes = _context.Themes.ToList();
            return View(model);
        }

        // GET: Theme/GetCss
        public ActionResult GetCss()
        {
            var theme = _themeService.GetActiveTheme();
            var css = _themeService.GetThemeCss(theme);

            return Content(css, "text/css");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _context?.Dispose();
                _authService?.Dispose();
                _themeService?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
