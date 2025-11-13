namespace FileSharePortal.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class AddFileDownloadTracking : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.FileDownloadLogs",
                c => new
                    {
                        DownloadLogId = c.Int(nullable: false, identity: true),
                        FileId = c.Int(nullable: false),
                        DownloadedByUserId = c.Int(nullable: false),
                        DownloadedDate = c.DateTime(nullable: false),
                        IpAddress = c.String(maxLength: 45),
                        UserAgent = c.String(maxLength: 500),
                    })
                .PrimaryKey(t => t.DownloadLogId)
                .ForeignKey("dbo.Users", t => t.DownloadedByUserId)
                .ForeignKey("dbo.SharedFiles", t => t.FileId)
                .Index(t => t.FileId)
                .Index(t => t.DownloadedByUserId);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.FileDownloadLogs", "FileId", "dbo.SharedFiles");
            DropForeignKey("dbo.FileDownloadLogs", "DownloadedByUserId", "dbo.Users");
            DropIndex("dbo.FileDownloadLogs", new[] { "DownloadedByUserId" });
            DropIndex("dbo.FileDownloadLogs", new[] { "FileId" });
            DropTable("dbo.FileDownloadLogs");
        }
    }
}
