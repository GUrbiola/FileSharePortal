namespace FileSharePortal.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class InitialCreate : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ApplicationExecutions",
                c => new
                    {
                        ExecutionId = c.Int(nullable: false, identity: true),
                        ApplicationId = c.Int(nullable: false),
                        StartTime = c.DateTime(nullable: false),
                        EndTime = c.DateTime(),
                        Status = c.Int(nullable: false),
                        ErrorMessage = c.String(),
                        ExecutionDetails = c.String(),
                        RecordsProcessed = c.Int(),
                    })
                .PrimaryKey(t => t.ExecutionId)
                .ForeignKey("dbo.Applications", t => t.ApplicationId, cascadeDelete: true)
                .Index(t => t.ApplicationId);
            
            CreateTable(
                "dbo.Applications",
                c => new
                    {
                        ApplicationId = c.Int(nullable: false, identity: true),
                        ApplicationName = c.String(nullable: false, maxLength: 200),
                        Description = c.String(maxLength: 1000),
                        StatusEndpoint = c.String(maxLength: 500),
                        LogPath = c.String(maxLength: 500),
                        CurrentStatus = c.Int(nullable: false),
                        LastStatusCheck = c.DateTime(),
                        LastSuccessfulRun = c.DateTime(),
                        IsActive = c.Boolean(nullable: false),
                        CheckIntervalMinutes = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.ApplicationId);
            
            CreateTable(
                "dbo.DistributionLists",
                c => new
                    {
                        DistributionListId = c.Int(nullable: false, identity: true),
                        Name = c.String(nullable: false, maxLength: 200),
                        ADDistinguishedName = c.String(nullable: false, maxLength: 500),
                        Description = c.String(maxLength: 500),
                        CreatedDate = c.DateTime(nullable: false),
                        LastSyncDate = c.DateTime(),
                    })
                .PrimaryKey(t => t.DistributionListId);
            
            CreateTable(
                "dbo.RoleDistributionLists",
                c => new
                    {
                        RoleDistributionListId = c.Int(nullable: false, identity: true),
                        RoleId = c.Int(nullable: false),
                        DistributionListId = c.Int(nullable: false),
                        AddedDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.RoleDistributionListId)
                .ForeignKey("dbo.DistributionLists", t => t.DistributionListId, cascadeDelete: true)
                .ForeignKey("dbo.Roles", t => t.RoleId, cascadeDelete: true)
                .Index(t => t.RoleId)
                .Index(t => t.DistributionListId);
            
            CreateTable(
                "dbo.Roles",
                c => new
                    {
                        RoleId = c.Int(nullable: false, identity: true),
                        RoleName = c.String(nullable: false, maxLength: 100),
                        Description = c.String(maxLength: 500),
                        CreatedDate = c.DateTime(nullable: false),
                        CreatedByUserId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.RoleId);
            
            CreateTable(
                "dbo.FileShares",
                c => new
                    {
                        FileShareId = c.Int(nullable: false, identity: true),
                        FileId = c.Int(nullable: false),
                        SharedWithUserId = c.Int(),
                        SharedWithRoleId = c.Int(),
                        SharedDate = c.DateTime(nullable: false),
                        SharedByUserId = c.Int(nullable: false),
                        CanDownload = c.Boolean(nullable: false),
                        ExpirationDate = c.DateTime(),
                    })
                .PrimaryKey(t => t.FileShareId)
                .ForeignKey("dbo.SharedFiles", t => t.FileId, cascadeDelete: true)
                .ForeignKey("dbo.Roles", t => t.SharedWithRoleId)
                .ForeignKey("dbo.Users", t => t.SharedWithUserId)
                .Index(t => t.FileId)
                .Index(t => t.SharedWithUserId)
                .Index(t => t.SharedWithRoleId);
            
            CreateTable(
                "dbo.SharedFiles",
                c => new
                    {
                        FileId = c.Int(nullable: false, identity: true),
                        FileName = c.String(nullable: false, maxLength: 500),
                        FilePath = c.String(nullable: false, maxLength: 1000),
                        ContentType = c.String(nullable: false, maxLength: 200),
                        FileSize = c.Long(nullable: false),
                        UploadedByUserId = c.Int(nullable: false),
                        UploadedDate = c.DateTime(nullable: false),
                        Description = c.String(maxLength: 1000),
                        IsDeleted = c.Boolean(nullable: false),
                        DeletedDate = c.DateTime(),
                        DeletedByUserId = c.Int(),
                        DownloadCount = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.FileId)
                .ForeignKey("dbo.Users", t => t.UploadedByUserId)
                .Index(t => t.UploadedByUserId);
            
            CreateTable(
                "dbo.FileReports",
                c => new
                    {
                        ReportId = c.Int(nullable: false, identity: true),
                        FileId = c.Int(nullable: false),
                        ReportedByUserId = c.Int(nullable: false),
                        Reason = c.String(nullable: false, maxLength: 200),
                        Details = c.String(nullable: false),
                        ReportedDate = c.DateTime(nullable: false),
                        Status = c.Int(nullable: false),
                        ReviewedByUserId = c.Int(),
                        ReviewedDate = c.DateTime(),
                        AdminNotes = c.String(maxLength: 1000),
                    })
                .PrimaryKey(t => t.ReportId)
                .ForeignKey("dbo.Users", t => t.ReportedByUserId, cascadeDelete: true)
                .ForeignKey("dbo.SharedFiles", t => t.FileId, cascadeDelete: true)
                .Index(t => t.FileId)
                .Index(t => t.ReportedByUserId);
            
            CreateTable(
                "dbo.Users",
                c => new
                    {
                        UserId = c.Int(nullable: false, identity: true),
                        Username = c.String(nullable: false, maxLength: 100),
                        PasswordHash = c.String(maxLength: 256),
                        FullName = c.String(nullable: false, maxLength: 200),
                        Email = c.String(nullable: false, maxLength: 200),
                        IsAdmin = c.Boolean(nullable: false),
                        IsActive = c.Boolean(nullable: false),
                        IsFromActiveDirectory = c.Boolean(nullable: false),
                        CreatedDate = c.DateTime(nullable: false),
                        LastLoginDate = c.DateTime(),
                    })
                .PrimaryKey(t => t.UserId)
                .Index(t => t.Username, unique: true);
            
            CreateTable(
                "dbo.Notifications",
                c => new
                    {
                        NotificationId = c.Int(nullable: false, identity: true),
                        UserId = c.Int(nullable: false),
                        Title = c.String(nullable: false, maxLength: 500),
                        Message = c.String(nullable: false),
                        Type = c.Int(nullable: false),
                        RelatedFileId = c.Int(),
                        RelatedApplicationId = c.Int(),
                        CreatedDate = c.DateTime(nullable: false),
                        IsRead = c.Boolean(nullable: false),
                        ReadDate = c.DateTime(),
                        ActionUrl = c.String(maxLength: 500),
                    })
                .PrimaryKey(t => t.NotificationId)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId);
            
            CreateTable(
                "dbo.RoleUsers",
                c => new
                    {
                        RoleUserId = c.Int(nullable: false, identity: true),
                        RoleId = c.Int(nullable: false),
                        UserId = c.Int(nullable: false),
                        AddedDate = c.DateTime(nullable: false),
                    })
                .PrimaryKey(t => t.RoleUserId)
                .ForeignKey("dbo.Roles", t => t.RoleId, cascadeDelete: true)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.RoleId)
                .Index(t => t.UserId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.RoleDistributionLists", "RoleId", "dbo.Roles");
            DropForeignKey("dbo.FileShares", "SharedWithUserId", "dbo.Users");
            DropForeignKey("dbo.FileShares", "SharedWithRoleId", "dbo.Roles");
            DropForeignKey("dbo.SharedFiles", "UploadedByUserId", "dbo.Users");
            DropForeignKey("dbo.FileShares", "FileId", "dbo.SharedFiles");
            DropForeignKey("dbo.FileReports", "FileId", "dbo.SharedFiles");
            DropForeignKey("dbo.RoleUsers", "UserId", "dbo.Users");
            DropForeignKey("dbo.RoleUsers", "RoleId", "dbo.Roles");
            DropForeignKey("dbo.Notifications", "UserId", "dbo.Users");
            DropForeignKey("dbo.FileReports", "ReportedByUserId", "dbo.Users");
            DropForeignKey("dbo.RoleDistributionLists", "DistributionListId", "dbo.DistributionLists");
            DropForeignKey("dbo.ApplicationExecutions", "ApplicationId", "dbo.Applications");
            DropIndex("dbo.RoleUsers", new[] { "UserId" });
            DropIndex("dbo.RoleUsers", new[] { "RoleId" });
            DropIndex("dbo.Notifications", new[] { "UserId" });
            DropIndex("dbo.Users", new[] { "Username" });
            DropIndex("dbo.FileReports", new[] { "ReportedByUserId" });
            DropIndex("dbo.FileReports", new[] { "FileId" });
            DropIndex("dbo.SharedFiles", new[] { "UploadedByUserId" });
            DropIndex("dbo.FileShares", new[] { "SharedWithRoleId" });
            DropIndex("dbo.FileShares", new[] { "SharedWithUserId" });
            DropIndex("dbo.FileShares", new[] { "FileId" });
            DropIndex("dbo.RoleDistributionLists", new[] { "DistributionListId" });
            DropIndex("dbo.RoleDistributionLists", new[] { "RoleId" });
            DropIndex("dbo.ApplicationExecutions", new[] { "ApplicationId" });
            DropTable("dbo.RoleUsers");
            DropTable("dbo.Notifications");
            DropTable("dbo.Users");
            DropTable("dbo.FileReports");
            DropTable("dbo.SharedFiles");
            DropTable("dbo.FileShares");
            DropTable("dbo.Roles");
            DropTable("dbo.RoleDistributionLists");
            DropTable("dbo.DistributionLists");
            DropTable("dbo.Applications");
            DropTable("dbo.ApplicationExecutions");
        }
    }
}
