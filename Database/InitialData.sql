
-- INSERT DEFAULT DATA
-- =============================================

-- Insert Default Admin User
-- Username: admin
-- Password: admin123
-- Password Hash generated using: BCrypt.Net.BCrypt.HashPassword("admin123")-> '$2a$11$8X0Y6xGP5K7qZ3mW4vF5h.kJNQh9rJYqTf2WXN8Y5P6mK4rS5L6Oy'
-- Alternative SHA256 Hash: 240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9
INSERT INTO [dbo].[Users] ([Username], [PasswordHash], [FullName], [Email], [IsAdmin], [IsActive], [IsFromActiveDirectory], CreatedDate)
--VALUES ('admin', '$2a$11$8X0Y6xGP5K7qZ3mW4vF5h.kJNQh9rJYqTf2WXN8Y5P6mK4rS5L6Oy', 'System Administrator', 'admin@fileshareportal.local', 1, 1, 0, getdate());
VALUES ('admin', '240be518fabd2724ddb6f04eeb1da5967448d7e831c08c8fa822809f74c720a9', 'System Administrator', 'admin@fileshareportal.local', 1, 1, 0, getdate());

GO

-- Get the admin user ID
DECLARE @AdminUserId INT;
SELECT @AdminUserId = UserId FROM [dbo].[Users] WHERE Username = 'admin';

-- Insert Default Themes
-- Theme 1: Default Blue
INSERT INTO [dbo].[Themes] ([ThemeName], [Description], [PrimaryColor], [PrimaryColorHover], [SecondaryColor], [SuccessColor], [DangerColor], [WarningColor], [InfoColor], [LightBackground], [DarkBackground], [SidebarBackground], [SidebarTextColor], [SidebarHoverBackground], [SidebarActiveBackground], [IsDefault], [IsActive], [CreatedByUserId])
VALUES ('Default Blue', 'Classic blue theme with professional appearance', '#0d6efd', '#0b5ed7', '#6c757d', '#198754', '#dc3545', '#ffc107', '#0dcaf0', '#f8f9fa', '#212529', '#ffffff', '#495057', '#f8f9fa', '#e7f3ff', 1, 1, @AdminUserId);

-- Theme 2: Dark Mode
INSERT INTO [dbo].[Themes] ([ThemeName], [Description], [PrimaryColor], [PrimaryColorHover], [SecondaryColor], [SuccessColor], [DangerColor], [WarningColor], [InfoColor], [LightBackground], [DarkBackground], [SidebarBackground], [SidebarTextColor], [SidebarHoverBackground], [SidebarActiveBackground], [IsDefault], [IsActive], [CreatedByUserId])
VALUES ('Dark Mode', 'Modern dark theme for reduced eye strain', '#375a7f', '#2c4866', '#444444', '#00bc8c', '#e74c3c', '#f39c12', '#3498db', '#343a40', '#1a1a1a', '#222222', '#adb5bd', '#2c3034', '#375a7f', 0, 0, @AdminUserId);

-- Theme 3: Forest Green
INSERT INTO [dbo].[Themes] ([ThemeName], [Description], [PrimaryColor], [PrimaryColorHover], [SecondaryColor], [SuccessColor], [DangerColor], [WarningColor], [InfoColor], [LightBackground], [DarkBackground], [SidebarBackground], [SidebarTextColor], [SidebarHoverBackground], [SidebarActiveBackground], [IsDefault], [IsActive], [CreatedByUserId])
VALUES ('Forest Green', 'Nature-inspired green theme', '#2d7a3e', '#245c30', '#6c757d', '#28a745', '#dc3545', '#ffc107', '#17a2b8', '#f0fff0', '#1a3d1f', '#f8fff8', '#2d5c36', '#e8f5e9', '#c8e6c9', 0, 0, @AdminUserId);

-- Theme 4: Royal Purple
INSERT INTO [dbo].[Themes] ([ThemeName], [Description], [PrimaryColor], [PrimaryColorHover], [SecondaryColor], [SuccessColor], [DangerColor], [WarningColor], [InfoColor], [LightBackground], [DarkBackground], [SidebarBackground], [SidebarTextColor], [SidebarHoverBackground], [SidebarActiveBackground], [IsDefault], [IsActive], [CreatedByUserId])
VALUES ('Royal Purple', 'Elegant purple theme with royal touch', '#6f42c1', '#5a32a3', '#6c757d', '#198754', '#dc3545', '#ffc107', '#0dcaf0', '#f5f0ff', '#2a1a40', '#faf8fc', '#4a2c70', '#f3e5f5', '#e1bee7', 0, 0, @AdminUserId);

-- Theme 5: Ocean Blue
INSERT INTO [dbo].[Themes] ([ThemeName], [Description], [PrimaryColor], [PrimaryColorHover], [SecondaryColor], [SuccessColor], [DangerColor], [WarningColor], [InfoColor], [LightBackground], [DarkBackground], [SidebarBackground], [SidebarTextColor], [SidebarHoverBackground], [SidebarActiveBackground], [IsDefault], [IsActive], [CreatedByUserId])
VALUES ('Ocean Blue', 'Deep ocean blue with calming effect', '#006ba6', '#004d75', '#6c757d', '#06d6a0', '#ef476f', '#ffd166', '#118ab2', '#e0f2fe', '#001f3f', '#e8f4f8', '#003d5b', '#d4e9f0', '#b3dae9', 0, 0, @AdminUserId);

-- Theme 6: Sunset Orange
INSERT INTO [dbo].[Themes] ([ThemeName], [Description], [PrimaryColor], [PrimaryColorHover], [SecondaryColor], [SuccessColor], [DangerColor], [WarningColor], [InfoColor], [LightBackground], [DarkBackground], [SidebarBackground], [SidebarTextColor], [SidebarHoverBackground], [SidebarActiveBackground], [IsDefault], [IsActive], [CreatedByUserId])
VALUES ('Sunset Orange', 'Warm sunset orange theme', '#ff6b35', '#e85a2a', '#6c757d', '#4ecdc4', '#c44536', '#f7b731', '#5f27cd', '#fff5f0', '#4a1f0f', '#fff8f0', '#cc4418', '#ffe8d6', '#ffd4b3', 0, 0, @AdminUserId);

-- Theme 7: Slate Gray
INSERT INTO [dbo].[Themes] ([ThemeName], [Description], [PrimaryColor], [PrimaryColorHover], [SecondaryColor], [SuccessColor], [DangerColor], [WarningColor], [InfoColor], [LightBackground], [DarkBackground], [SidebarBackground], [SidebarTextColor], [SidebarHoverBackground], [SidebarActiveBackground], [IsDefault], [IsActive], [CreatedByUserId])
VALUES ('Slate Gray', 'Professional gray theme for corporate environment', '#475569', '#334155', '#64748b', '#22c55e', '#ef4444', '#f59e0b', '#3b82f6', '#f1f5f9', '#1e293b', '#f8fafc', '#1e293b', '#e2e8f0', '#cbd5e1', 0, 0, @AdminUserId);

-- Theme 8: Ruby Red
INSERT INTO [dbo].[Themes] ([ThemeName], [Description], [PrimaryColor], [PrimaryColorHover], [SecondaryColor], [SuccessColor], [DangerColor], [WarningColor], [InfoColor], [LightBackground], [DarkBackground], [SidebarBackground], [SidebarTextColor], [SidebarHoverBackground], [SidebarActiveBackground], [IsDefault], [IsActive], [CreatedByUserId])
VALUES ('Ruby Red', 'Bold red theme with strong presence', '#b91c1c', '#991b1b', '#6c757d', '#059669', '#dc2626', '#f59e0b', '#06b6d4', '#fef2f2', '#450a0a', '#fef2f2', '#7f1d1d', '#fee2e2', '#fecaca', 0, 0, @AdminUserId);

-- Theme 9: Mint Fresh
INSERT INTO [dbo].[Themes] ([ThemeName], [Description], [PrimaryColor], [PrimaryColorHover], [SecondaryColor], [SuccessColor], [DangerColor], [WarningColor], [InfoColor], [LightBackground], [DarkBackground], [SidebarBackground], [SidebarTextColor], [SidebarHoverBackground], [SidebarActiveBackground], [IsDefault], [IsActive], [CreatedByUserId])
VALUES ('Mint Fresh', 'Fresh mint green theme for modern look', '#10b981', '#059669', '#6c757d', '#14b8a6', '#f43f5e', '#fb923c', '#06b6d4', '#ecfdf5', '#022c22', '#f0fdf4', '#065f46', '#dcfce7', '#bbf7d0', 0, 0, @AdminUserId);

-- Theme 10: Corporate Blue
INSERT INTO [dbo].[Themes] ([ThemeName], [Description], [PrimaryColor], [PrimaryColorHover], [SecondaryColor], [SuccessColor], [DangerColor], [WarningColor], [InfoColor], [LightBackground], [DarkBackground], [SidebarBackground], [SidebarTextColor], [SidebarHoverBackground], [SidebarActiveBackground], [IsDefault], [IsActive], [CreatedByUserId])
VALUES ('Corporate Blue', 'Traditional corporate blue theme', '#1e40af', '#1e3a8a', '#6c757d', '#16a34a', '#dc2626', '#ea580c', '#0284c7', '#eff6ff', '#172554', '#eff6ff', '#1e3a8a', '#dbeafe', '#bfdbfe', 0, 0, @AdminUserId);

-- Theme 11: Midnight Black
INSERT INTO [dbo].[Themes] ([ThemeName], [Description], [PrimaryColor], [PrimaryColorHover], [SecondaryColor], [SuccessColor], [DangerColor], [WarningColor], [InfoColor], [LightBackground], [DarkBackground], [SidebarBackground], [SidebarTextColor], [SidebarHoverBackground], [SidebarActiveBackground], [IsDefault], [IsActive], [CreatedByUserId])
VALUES ('Midnight Black', 'Ultra-dark theme for night work', '#1f2937', '#111827', '#4b5563', '#10b981', '#ef4444', '#f59e0b', '#3b82f6', '#374151', '#030712', '#0f172a', '#d1d5db', '#1e293b', '#334155', 0, 0, @AdminUserId);

-- Theme 12: Lavender Dream
INSERT INTO [dbo].[Themes] ([ThemeName], [Description], [PrimaryColor], [PrimaryColorHover], [SecondaryColor], [SuccessColor], [DangerColor], [WarningColor], [InfoColor], [LightBackground], [DarkBackground], [SidebarBackground], [SidebarTextColor], [SidebarHoverBackground], [SidebarActiveBackground], [IsDefault], [IsActive], [CreatedByUserId])
VALUES ('Lavender Dream', 'Soft lavender theme with gentle colors', '#8b5cf6', '#7c3aed', '#6c757d', '#10b981', '#f43f5e', '#fb923c', '#06b6d4', '#faf5ff', '#3b0764', '#faf5ff', '#5b21b6', '#f3e8ff', '#e9d5ff', 0, 0, @AdminUserId);

GO

-- Get the ThemeId of the default active theme (Default Blue)
DECLARE @DefaultThemeId INT;
SELECT @DefaultThemeId = ThemeId FROM [dbo].[Themes] WHERE ThemeName = 'Default Blue';

DECLARE @AdminUserId2 INT;
SELECT @AdminUserId2 = UserId FROM [dbo].[Users] WHERE Username = 'admin';

-- Insert Default Site Settings
INSERT INTO [dbo].[SiteSettings] ([SiteName], [SiteDescription], [ActiveThemeId], [LastModifiedByUserId], [LastModifiedDate])
VALUES ('File Share Portal', 'Secure file sharing and collaboration platform', @DefaultThemeId, @AdminUserId2, GETDATE());
GO

-- =============================================
-- VERIFICATION QUERIES
-- =============================================
PRINT 'Database initialization completed successfully!';
PRINT '';
PRINT 'Summary:';
PRINT '- Admin User Created: admin / admin123';
declare @tc integer
SELECT @tc = COUNT(*) FROM [dbo].[Themes]
PRINT '- Themes Created: ' + CONVERT (VARCHAR(10), @tc);
PRINT '- Site Settings Configured';
PRINT '';
PRINT 'Available Themes:';
SELECT ThemeId, ThemeName, Description,
       CASE WHEN IsActive = 1 THEN 'ACTIVE' ELSE '' END AS Status
FROM [dbo].[Themes]
ORDER BY ThemeId;
GO
