namespace FileSharePortal.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class API : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ApiTokens",
                c => new
                    {
                        TokenId = c.Int(nullable: false, identity: true),
                        Token = c.String(nullable: false, maxLength: 500),
                        UserId = c.Int(nullable: false),
                        ApplicationId = c.Int(),
                        CreatedDate = c.DateTime(nullable: false),
                        ExpiresDate = c.DateTime(nullable: false),
                        IsRevoked = c.Boolean(nullable: false),
                        LastUsedDate = c.DateTime(),
                        IpAddress = c.String(maxLength: 100),
                    })
                .PrimaryKey(t => t.TokenId)
                .ForeignKey("dbo.Applications", t => t.ApplicationId)
                .ForeignKey("dbo.Users", t => t.UserId, cascadeDelete: true)
                .Index(t => t.UserId)
                .Index(t => t.ApplicationId);
            
            CreateTable(
                "dbo.ApplicationLogFiles",
                c => new
                    {
                        LogFileId = c.Int(nullable: false, identity: true),
                        ExecutionId = c.Int(nullable: false),
                        FileName = c.String(nullable: false, maxLength: 255),
                        ContentType = c.String(nullable: false, maxLength: 100),
                        FileSize = c.Long(nullable: false),
                        FileContent = c.Binary(nullable: false),
                        UploadedDate = c.DateTime(nullable: false),
                        Description = c.String(maxLength: 500),
                    })
                .PrimaryKey(t => t.LogFileId)
                .ForeignKey("dbo.ApplicationExecutions", t => t.ExecutionId, cascadeDelete: true)
                .Index(t => t.ExecutionId);
            
            AddColumn("dbo.ApplicationExecutions", "ExecutedByUserId", c => c.Int());
            AddColumn("dbo.Applications", "ApiKey", c => c.String(maxLength: 500));
            AddColumn("dbo.Applications", "RegisteredByUserId", c => c.Int());
            AddColumn("dbo.Applications", "RegisteredDate", c => c.DateTime());
            AddColumn("dbo.Applications", "ContactEmail", c => c.String(maxLength: 200));
            CreateIndex("dbo.ApplicationExecutions", "ExecutedByUserId");
            AddForeignKey("dbo.ApplicationExecutions", "ExecutedByUserId", "dbo.Users", "UserId");
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.ApiTokens", "UserId", "dbo.Users");
            DropForeignKey("dbo.ApplicationLogFiles", "ExecutionId", "dbo.ApplicationExecutions");
            DropForeignKey("dbo.ApplicationExecutions", "ExecutedByUserId", "dbo.Users");
            DropForeignKey("dbo.ApiTokens", "ApplicationId", "dbo.Applications");
            DropIndex("dbo.ApplicationLogFiles", new[] { "ExecutionId" });
            DropIndex("dbo.ApplicationExecutions", new[] { "ExecutedByUserId" });
            DropIndex("dbo.ApiTokens", new[] { "ApplicationId" });
            DropIndex("dbo.ApiTokens", new[] { "UserId" });
            DropColumn("dbo.Applications", "ContactEmail");
            DropColumn("dbo.Applications", "RegisteredDate");
            DropColumn("dbo.Applications", "RegisteredByUserId");
            DropColumn("dbo.Applications", "ApiKey");
            DropColumn("dbo.ApplicationExecutions", "ExecutedByUserId");
            DropTable("dbo.ApplicationLogFiles");
            DropTable("dbo.ApiTokens");
        }
    }
}
