namespace FileSharePortal.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class AddErrorLogging : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.ErrorLogs",
                c => new
                    {
                        ErrorLogId = c.Int(nullable: false, identity: true),
                        ErrorMessage = c.String(nullable: false),
                        InnerException = c.String(),
                        StackTrace = c.String(),
                        OccurredAt = c.DateTime(nullable: false),
                        RequestUrl = c.String(maxLength: 500),
                        HttpMethod = c.String(maxLength: 50),
                        UserAgent = c.String(maxLength: 200),
                        IpAddress = c.String(maxLength: 100),
                        UserId = c.Int(),
                        Username = c.String(maxLength: 200),
                        ControllerName = c.String(maxLength: 100),
                        ActionName = c.String(maxLength: 100),
                        ExceptionType = c.String(maxLength: 500),
                        IsResolved = c.Boolean(nullable: false),
                        ResolvedAt = c.DateTime(),
                        ResolvedByUserId = c.Int(),
                        ResolutionNotes = c.String(maxLength: 1000),
                    })
                .PrimaryKey(t => t.ErrorLogId)
                .ForeignKey("dbo.Users", t => t.UserId)
                .Index(t => t.UserId)
                .Index(t => t.OccurredAt);
        }

        public override void Down()
        {
            DropForeignKey("dbo.ErrorLogs", "UserId", "dbo.Users");
            DropIndex("dbo.ErrorLogs", new[] { "OccurredAt" });
            DropIndex("dbo.ErrorLogs", new[] { "UserId" });
            DropTable("dbo.ErrorLogs");
        }
    }
}
