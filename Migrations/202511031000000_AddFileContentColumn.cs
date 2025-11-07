namespace FileSharePortal.Migrations
{
    using System;
    using System.Data.Entity.Migrations;

    public partial class AddFileContentColumn : DbMigration
    {
        public override void Up()
        {
            // Add FileContent column to store file data in database
            AddColumn("dbo.SharedFiles", "FileContent", c => c.Binary());

            // Make FilePath nullable since we'll store content in DB instead
            AlterColumn("dbo.SharedFiles", "FilePath", c => c.String(maxLength: 1000));
        }

        public override void Down()
        {
            // Remove FileContent column
            DropColumn("dbo.SharedFiles", "FileContent");

            // Restore FilePath to required
            AlterColumn("dbo.SharedFiles", "FilePath", c => c.String(nullable: false, maxLength: 1000));
        }
    }
}
