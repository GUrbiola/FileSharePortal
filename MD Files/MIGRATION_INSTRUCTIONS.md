# Database Migration Instructions for FileDownloadLog Feature

## Overview
This guide will help you create and apply a database migration to add the `FileDownloadLogs` table to your database.

---

## Step 1: Open Package Manager Console in Visual Studio

1. Open your solution in **Visual Studio**
2. Go to **Tools** → **NuGet Package Manager** → **Package Manager Console**
3. The Package Manager Console will appear (usually at the bottom of Visual Studio)

---

## Step 2: Verify Your Default Project

In the Package Manager Console, make sure the **Default project** dropdown is set to:
```
FileSharePortal
```

This ensures the migration is created in the correct project.

---

## Step 3: Create the Migration

In the Package Manager Console, run the following command:

```powershell
Add-Migration AddFileDownloadTracking
```

**What this does:**
- Analyzes your current model (including the new `FileDownloadLog` class)
- Compares it to the last migration snapshot
- Generates a new migration file with the necessary changes
- Creates three files in the `Migrations` folder:
  - `[Timestamp]_AddFileDownloadTracking.cs` - The migration code
  - `[Timestamp]_AddFileDownloadTracking.Designer.cs` - Designer metadata
  - `[Timestamp]_AddFileDownloadTracking.resx` - Resource file with model snapshot

**Expected Output:**
```
Scaffolding migration 'AddFileDownloadTracking'.
The Designer Code for this migration file includes a snapshot of your current Code First model.
This snapshot is used to calculate the changes to your model when you scaffold the next migration.
If you make additional changes to your model that you want to include in this migration,
then you can re-scaffold it by running 'Add-Migration AddFileDownloadTracking' again.
```

---

## Step 4: Review the Migration (Optional but Recommended)

1. Navigate to the `Migrations` folder in Solution Explorer
2. Open the newly created `[Timestamp]_AddFileDownloadTracking.cs` file
3. Review the `Up()` and `Down()` methods to ensure they look correct:

**Expected Up() method should contain:**
- `CreateTable("dbo.FileDownloadLogs", ...)`
- Foreign keys to `Users` and `SharedFiles` tables
- Indexes on `FileId`, `DownloadedByUserId`, and `DownloadedDate`

**Expected Down() method should contain:**
- Code to reverse the changes (drop table, indexes, and foreign keys)

---

## Step 5: Apply the Migration to Your Database

### Option A: Update to Latest Migration (Recommended)

In the Package Manager Console, run:

```powershell
Update-Database
```

This applies ALL pending migrations to your database.

**Expected Output:**
```
Specify the '-Verbose' flag to view the SQL statements being applied to the target database.
Applying explicit migrations: [202511111300000_AddFileDownloadTracking].
Applying explicit migration: 202511111300000_AddFileDownloadTracking.
Running Seed method.
```

### Option B: Update with Verbose Output (See SQL Commands)

```powershell
Update-Database -Verbose
```

This shows you the exact SQL commands being executed.

### Option C: Generate SQL Script (For Production Environments)

If you want to review or manually run the SQL script:

```powershell
Update-Database -Script -SourceMigration: $InitialDatabase
```

This generates a SQL script file that you can review and run manually.

---

## Step 6: Verify the Migration Was Successful

### Verify in Package Manager Console:

Run this command to see the list of applied migrations:

```powershell
Get-Migrations
```

You should see `AddFileDownloadTracking` in the list.

### Verify in SQL Server:

1. Open **SQL Server Object Explorer** in Visual Studio (View → SQL Server Object Explorer)
2. Navigate to your database
3. Expand **Tables**
4. You should see a new table: `dbo.FileDownloadLogs`
5. Expand it to verify it has the correct columns:
   - `DownloadLogId` (int, PK, Identity)
   - `FileId` (int, FK to SharedFiles)
   - `DownloadedByUserId` (int, FK to Users)
   - `DownloadedDate` (datetime)
   - `IpAddress` (varchar(45))
   - `UserAgent` (varchar(500))

---

## Troubleshooting

### Issue: "No migrations configuration type was found..."

**Solution:** Make sure your Default Project in Package Manager Console is set to `FileSharePortal`.

---

### Issue: "Unable to generate an explicit migration..."

**Solution:**
1. Clean and rebuild your solution (Build → Clean Solution, then Build → Build Solution)
2. Close and reopen Visual Studio
3. Try the `Add-Migration` command again

---

### Issue: "The model backing the context has changed..."

**Solution:** This error occurs if you've made model changes without creating a migration. Run:

```powershell
Add-Migration AddFileDownloadTracking -Force
```

The `-Force` flag will overwrite an existing migration with the same name.

---

### Issue: Database connection errors

**Solution:**
1. Verify your connection string in `Web.config`
2. Make sure SQL Server is running
3. Check that you have permissions to modify the database

---

## Rolling Back a Migration (If Needed)

### To Undo the Last Migration:

```powershell
Update-Database -TargetMigration: [PreviousMigrationName]
```

Replace `[PreviousMigrationName]` with the name of the migration before `AddFileDownloadTracking`.

### To Completely Remove a Migration (Before Applying):

```powershell
Remove-Migration
```

**Warning:** This only works if the migration hasn't been applied to the database yet!

---

## Alternative: Manual Migration Creation

If you prefer to manually create the migration file, here's the code:

**File:** `Migrations/202511111300000_AddFileDownloadTracking.cs`

```csharp
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
                .ForeignKey("dbo.SharedFiles", t => t.FileId)
                .ForeignKey("dbo.Users", t => t.DownloadedByUserId)
                .Index(t => t.FileId)
                .Index(t => t.DownloadedByUserId)
                .Index(t => t.DownloadedDate);
        }

        public override void Down()
        {
            DropForeignKey("dbo.FileDownloadLogs", "DownloadedByUserId", "dbo.Users");
            DropForeignKey("dbo.FileDownloadLogs", "FileId", "dbo.SharedFiles");
            DropIndex("dbo.FileDownloadLogs", new[] { "DownloadedDate" });
            DropIndex("dbo.FileDownloadLogs", new[] { "DownloadedByUserId" });
            DropIndex("dbo.FileDownloadLogs", new[] { "FileId" });
            DropTable("dbo.FileDownloadLogs");
        }
    }
}
```

**Note:** If using manual migration, you still need to run `Update-Database` to apply it.

---

## Summary - Quick Steps

For most users, these are the only commands you need:

```powershell
# 1. Create the migration
Add-Migration AddFileDownloadTracking

# 2. Apply the migration
Update-Database

# 3. (Optional) Verify migrations
Get-Migrations
```

---

## After Migration is Complete

Once the migration is successfully applied:

1. ✅ The `FileDownloadLogs` table will be created in your database
2. ✅ All file downloads will now be automatically tracked
3. ✅ File owners and admins can view download history in the file details page
4. ✅ Download history includes: who, when, IP address, and user agent

---

## Need More Help?

- **Entity Framework Migrations Documentation:** https://docs.microsoft.com/en-us/ef/ef6/modeling/code-first/migrations/
- **Package Manager Console Commands:** https://docs.microsoft.com/en-us/ef/ef6/modeling/code-first/migrations/commands

---

**Created:** 2025-11-11
**Feature:** File Download Tracking System
