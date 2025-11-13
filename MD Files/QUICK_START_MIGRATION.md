# Quick Start: Database Migration for Download Tracking

## Choose Your Method

### ⚡ Method 1: Entity Framework Migrations (RECOMMENDED)

**Use this if:** You're familiar with Entity Framework and want automatic migration management.

#### Steps:
1. Open **Visual Studio**
2. Go to **Tools** → **NuGet Package Manager** → **Package Manager Console**
3. Run these commands:

```powershell
Add-Migration AddFileDownloadTracking
Update-Database
```

✅ **Done!** The database is updated.

📖 **Need detailed instructions?** See [MIGRATION_INSTRUCTIONS.md](MIGRATION_INSTRUCTIONS.md)

---

### 🔧 Method 2: Manual SQL Script

**Use this if:** You prefer running SQL manually or deploying to production.

#### Steps:
1. Open **SQL Server Management Studio** (or SQL Server Object Explorer in Visual Studio)
2. Connect to your database
3. Open the file: `MIGRATION_SQL_SCRIPT.sql`
4. Review the script
5. Execute it

✅ **Done!** The database is updated.

---

## Verify the Migration

### Check in Visual Studio:
1. Open **View** → **SQL Server Object Explorer**
2. Expand your database → **Tables**
3. Look for `dbo.FileDownloadLogs` table

### Check the Table Structure:
The table should have these columns:
- ✅ `DownloadLogId` (Primary Key)
- ✅ `FileId` (Foreign Key to SharedFiles)
- ✅ `DownloadedByUserId` (Foreign Key to Users)
- ✅ `DownloadedDate`
- ✅ `IpAddress`
- ✅ `UserAgent`

---

## What Happens Next?

After the migration:

1. **Automatic Tracking**: Every file download is automatically logged
2. **Download History**: File owners and admins can view detailed download logs
3. **Audit Trail**: Track who downloaded files, when, and from where

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| "No migrations configuration found" | Set Default Project to `FileSharePortal` in Package Manager Console |
| Database connection error | Check your `Web.config` connection string |
| Migration already exists | Run `Add-Migration AddFileDownloadTracking -Force` |
| Need to undo migration | Run `Update-Database -TargetMigration: [PreviousMigrationName]` |

---

## Need Help?

- 📖 **Full Instructions**: [MIGRATION_INSTRUCTIONS.md](MIGRATION_INSTRUCTIONS.md)
- 📜 **SQL Script**: [MIGRATION_SQL_SCRIPT.sql](MIGRATION_SQL_SCRIPT.sql)
- 🌐 **EF Documentation**: https://docs.microsoft.com/en-us/ef/ef6/modeling/code-first/migrations/

---

**Ready to go?** Choose your method above and get started! 🚀
