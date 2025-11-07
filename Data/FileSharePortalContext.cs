using System.Data.Entity;
using FileSharePortal.Models;

namespace FileSharePortal.Data
{
    public class FileSharePortalContext : DbContext
    {
        public FileSharePortalContext() : base("FileSharePortalContext")
        {
            Database.SetInitializer(new FileSharePortalInitializer());
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<DistributionList> DistributionLists { get; set; }
        public DbSet<RoleUser> RoleUsers { get; set; }
        public DbSet<RoleDistributionList> RoleDistributionLists { get; set; }
        public DbSet<SharedFile> SharedFiles { get; set; }
        public DbSet<FileShare> FileShares { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<FileReport> FileReports { get; set; }
        public DbSet<Application> Applications { get; set; }
        public DbSet<ApplicationExecution> ApplicationExecutions { get; set; }
        public DbSet<ApplicationLogFile> ApplicationLogFiles { get; set; }
        public DbSet<ApiToken> ApiTokens { get; set; }
        public DbSet<Theme> Themes { get; set; }
        public DbSet<SiteSettings> SiteSettings { get; set; }
        public DbSet<ErrorLog> ErrorLogs { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<FileShare>()
                .HasOptional(fs => fs.SharedWithUser)
                .WithMany()
                .HasForeignKey(fs => fs.SharedWithUserId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<FileShare>()
                .HasOptional(fs => fs.SharedWithRole)
                .WithMany(r => r.FileShares)
                .HasForeignKey(fs => fs.SharedWithRoleId)
                .WillCascadeOnDelete(false);

            modelBuilder.Entity<SharedFile>()
                .HasRequired(sf => sf.UploadedBy)
                .WithMany(u => u.UploadedFiles)
                .HasForeignKey(sf => sf.UploadedByUserId)
                .WillCascadeOnDelete(false);
        }
    }

    public class FileSharePortalInitializer : CreateDatabaseIfNotExists<FileSharePortalContext>
    {
        protected override void Seed(FileSharePortalContext context)
        {
            // Create default admin user
            var adminUser = new User
            {
                Username = "admin",
                PasswordHash = HashPassword("admin123"),
                FullName = "System Administrator",
                Email = "admin@fileshareportal.local",
                IsAdmin = true,
                IsActive = true,
                IsFromActiveDirectory = false
            };

            context.Users.Add(adminUser);
            context.SaveChanges();

            // Create default themes
            var defaultTheme = new Theme
            {
                ThemeId = 1,
                ThemeName = "Default Blue",
                Description = "Default blue theme",
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
                SidebarActiveBackground = "#e7f3ff",
                IsDefault = true,
                IsActive = true
            };

            var darkTheme = new Theme
            {
                ThemeId = 2,
                ThemeName = "Dark Mode",
                Description = "Dark theme for reduced eye strain",
                PrimaryColor = "#0d6efd",
                PrimaryColorHover = "#0b5ed7",
                SecondaryColor = "#6c757d",
                SuccessColor = "#198754",
                DangerColor = "#dc3545",
                WarningColor = "#ffc107",
                InfoColor = "#0dcaf0",
                LightBackground = "#2b2b2b",
                DarkBackground = "#1a1a1a",
                SidebarBackground = "#2b2b2b",
                SidebarTextColor = "#e0e0e0",
                SidebarHoverBackground = "#3a3a3a",
                SidebarActiveBackground = "#0d6efd",
                IsDefault = false,
                IsActive = false
            };

            var greenTheme = new Theme
            {
                ThemeId = 3,
                ThemeName = "Forest Green",
                Description = "Green nature-inspired theme",
                PrimaryColor = "#198754",
                PrimaryColorHover = "#157347",
                SecondaryColor = "#6c757d",
                SuccessColor = "#28a745",
                DangerColor = "#dc3545",
                WarningColor = "#ffc107",
                InfoColor = "#17a2b8",
                LightBackground = "#f8f9fa",
                DarkBackground = "#212529",
                SidebarBackground = "#ffffff",
                SidebarTextColor = "#495057",
                SidebarHoverBackground = "#f8f9fa",
                SidebarActiveBackground = "#d1f4e0",
                IsDefault = false,
                IsActive = false
            };

            var purpleTheme = new Theme
            {
                ThemeId = 4,
                ThemeName = "Royal Purple",
                Description = "Elegant purple theme",
                PrimaryColor = "#6f42c1",
                PrimaryColorHover = "#5a32a3",
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
                SidebarActiveBackground = "#e7d6fd",
                IsDefault = false,
                IsActive = false
            };

            context.Themes.Add(defaultTheme);
            context.Themes.Add(darkTheme);
            context.Themes.Add(greenTheme);
            context.Themes.Add(purpleTheme);
            context.SaveChanges();

            // Create default site settings
            var siteSettings = new SiteSettings
            {
                SiteName = "File Share Portal",
                SiteDescription = "Secure file sharing and collaboration platform",
                ActiveThemeId = defaultTheme.ThemeId
            };

            context.SiteSettings.Add(siteSettings);
            context.SaveChanges();

            base.Seed(context);
        }

        private string HashPassword(string password)
        {
            using (var sha256 = System.Security.Cryptography.SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
                return System.BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }
    }


}
