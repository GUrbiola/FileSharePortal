namespace FileSharePortal.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Themes : DbMigration
    {
        public override void Up()
        {
            DropPrimaryKey("dbo.SiteSettings");
            AddColumn("dbo.SiteSettings", "SettingsId", c => c.Int(nullable: false, identity: true));
            AddColumn("dbo.SiteSettings", "FaviconPath", c => c.String());
            AddColumn("dbo.SiteSettings", "LogoPath", c => c.String());
            AddColumn("dbo.SiteSettings", "LastModifiedByUserId", c => c.Int(nullable: false));
            AddColumn("dbo.SiteSettings", "LastModifiedDate", c => c.DateTime(nullable: false));
            AddColumn("dbo.Themes", "CreatedByUserId", c => c.Int(nullable: false));
            AddPrimaryKey("dbo.SiteSettings", "SettingsId");
            DropColumn("dbo.SiteSettings", "SiteSettingsId");
        }
        
        public override void Down()
        {
            AddColumn("dbo.SiteSettings", "SiteSettingsId", c => c.Int(nullable: false, identity: true));
            DropPrimaryKey("dbo.SiteSettings");
            DropColumn("dbo.Themes", "CreatedByUserId");
            DropColumn("dbo.SiteSettings", "LastModifiedDate");
            DropColumn("dbo.SiteSettings", "LastModifiedByUserId");
            DropColumn("dbo.SiteSettings", "LogoPath");
            DropColumn("dbo.SiteSettings", "FaviconPath");
            DropColumn("dbo.SiteSettings", "SettingsId");
            AddPrimaryKey("dbo.SiteSettings", "SiteSettingsId");
        }
    }
}
