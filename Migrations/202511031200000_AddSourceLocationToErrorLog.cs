namespace FileSharePortal.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class AddSourceLocationToErrorLog : DbMigration
    {
        public override void Up()
        {
            AddColumn("dbo.ErrorLogs", "SourceFile", c => c.String(maxLength: 500));
            AddColumn("dbo.ErrorLogs", "LineNumber", c => c.Int());
        }

        public override void Down()
        {
            DropColumn("dbo.ErrorLogs", "LineNumber");
            DropColumn("dbo.ErrorLogs", "SourceFile");
        }
    }
}
