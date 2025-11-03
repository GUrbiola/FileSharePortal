namespace FileSharePortal.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class ThemesAdded : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.SiteSettings",
                c => new
                    {
                        SiteSettingsId = c.Int(nullable: false, identity: true),
                        SiteName = c.String(nullable: false),
                        SiteDescription = c.String(nullable: false),
                        ActiveThemeId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.SiteSettingsId);
            
            CreateTable(
                "dbo.Themes",
                c => new
                    {
                        ThemeId = c.Int(nullable: false, identity: true),
                        ThemeName = c.String(nullable: false, maxLength: 50),
                        Description = c.String(nullable: false, maxLength: 200),
                        PrimaryColor = c.String(nullable: false, maxLength: 10),
                        PrimaryColorHover = c.String(nullable: false, maxLength: 10),
                        SecondaryColor = c.String(nullable: false, maxLength: 10),
                        SuccessColor = c.String(nullable: false, maxLength: 10),
                        DangerColor = c.String(nullable: false, maxLength: 10),
                        WarningColor = c.String(nullable: false, maxLength: 10),
                        InfoColor = c.String(nullable: false, maxLength: 10),
                        LightBackground = c.String(nullable: false, maxLength: 10),
                        DarkBackground = c.String(nullable: false, maxLength: 10),
                        SidebarBackground = c.String(nullable: false, maxLength: 10),
                        SidebarTextColor = c.String(nullable: false, maxLength: 10),
                        SidebarHoverBackground = c.String(nullable: false, maxLength: 10),
                        SidebarActiveBackground = c.String(nullable: false, maxLength: 10),
                        IsDefault = c.Boolean(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                    })
                .PrimaryKey(t => t.ThemeId);
            
        }
        
        public override void Down()
        {
            DropTable("dbo.Themes");
            DropTable("dbo.SiteSettings");
        }
    }
}
