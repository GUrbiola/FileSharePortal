/*
================================================================================
File Download Tracking Migration Script
================================================================================
Description: Creates the FileDownloadLogs table to track file downloads
Created: 2025-11-11
Author: File Share Portal

IMPORTANT: This is an ALTERNATIVE to using Entity Framework migrations.
Use this script if:
  - You prefer manual SQL execution
  - You're deploying to production
  - Entity Framework migrations are not working

Before running:
  1. Review the script
  2. Backup your database
  3. Ensure you're connected to the correct database
================================================================================
*/

-- Check if table already exists
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FileDownloadLogs]') AND type in (N'U'))
BEGIN
    PRINT 'Creating FileDownloadLogs table...'

    -- Create the FileDownloadLogs table
    CREATE TABLE [dbo].[FileDownloadLogs] (
        [DownloadLogId] INT IDENTITY(1,1) NOT NULL,
        [FileId] INT NOT NULL,
        [DownloadedByUserId] INT NOT NULL,
        [DownloadedDate] DATETIME NOT NULL,
        [IpAddress] NVARCHAR(45) NULL,
        [UserAgent] NVARCHAR(500) NULL,

        -- Primary Key
        CONSTRAINT [PK_dbo.FileDownloadLogs] PRIMARY KEY CLUSTERED ([DownloadLogId] ASC),

        -- Foreign Key to SharedFiles table
        CONSTRAINT [FK_dbo.FileDownloadLogs_dbo.SharedFiles_FileId]
            FOREIGN KEY([FileId]) REFERENCES [dbo].[SharedFiles] ([FileId]),

        -- Foreign Key to Users table
        CONSTRAINT [FK_dbo.FileDownloadLogs_dbo.Users_DownloadedByUserId]
            FOREIGN KEY([DownloadedByUserId]) REFERENCES [dbo].[Users] ([UserId])
    );

    PRINT 'FileDownloadLogs table created successfully.'
END
ELSE
BEGIN
    PRINT 'FileDownloadLogs table already exists. Skipping creation.'
END
GO

-- Create indexes for better query performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_FileId' AND object_id = OBJECT_ID(N'[dbo].[FileDownloadLogs]'))
BEGIN
    PRINT 'Creating index on FileId...'
    CREATE NONCLUSTERED INDEX [IX_FileId] ON [dbo].[FileDownloadLogs]
    (
        [FileId] ASC
    );
    PRINT 'Index IX_FileId created successfully.'
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DownloadedByUserId' AND object_id = OBJECT_ID(N'[dbo].[FileDownloadLogs]'))
BEGIN
    PRINT 'Creating index on DownloadedByUserId...'
    CREATE NONCLUSTERED INDEX [IX_DownloadedByUserId] ON [dbo].[FileDownloadLogs]
    (
        [DownloadedByUserId] ASC
    );
    PRINT 'Index IX_DownloadedByUserId created successfully.'
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_DownloadedDate' AND object_id = OBJECT_ID(N'[dbo].[FileDownloadLogs]'))
BEGIN
    PRINT 'Creating index on DownloadedDate...'
    CREATE NONCLUSTERED INDEX [IX_DownloadedDate] ON [dbo].[FileDownloadLogs]
    (
        [DownloadedDate] DESC
    );
    PRINT 'Index IX_DownloadedDate created successfully.'
END
GO

-- Update the __MigrationHistory table (if using Entity Framework)
-- Note: You may need to adjust the timestamp and migration name
-- Uncomment the following lines if you want to record this in EF migration history

/*
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[__MigrationHistory]') AND type in (N'U'))
BEGIN
    DECLARE @MigrationId NVARCHAR(150) = '202511111300000_AddFileDownloadTracking'
    DECLARE @ContextKey NVARCHAR(300) = 'FileSharePortal.Data.FileSharePortalContext'

    IF NOT EXISTS (SELECT * FROM [dbo].[__MigrationHistory] WHERE [MigrationId] = @MigrationId AND [ContextKey] = @ContextKey)
    BEGIN
        INSERT INTO [dbo].[__MigrationHistory] ([MigrationId], [ContextKey], [Model], [ProductVersion])
        VALUES (@MigrationId, @ContextKey, 0x1F8B0800000000000400, '6.4.4')

        PRINT 'Migration history updated.'
    END
END
*/

GO

-- Verification Query
PRINT ''
PRINT '================================================================================';
PRINT 'Verification'
PRINT '================================================================================';

-- Check if table exists
IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FileDownloadLogs]') AND type in (N'U'))
BEGIN
    PRINT '✓ Table [FileDownloadLogs] exists'

    -- Show table structure
    PRINT ''
    PRINT 'Table Structure:'
    SELECT
        c.name AS ColumnName,
        t.name AS DataType,
        c.max_length AS MaxLength,
        c.is_nullable AS IsNullable,
        c.is_identity AS IsIdentity
    FROM sys.columns c
    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID(N'[dbo].[FileDownloadLogs]')
    ORDER BY c.column_id;

    -- Show foreign keys
    PRINT ''
    PRINT 'Foreign Keys:'
    SELECT
        fk.name AS ForeignKeyName,
        OBJECT_NAME(fk.parent_object_id) AS TableName,
        COL_NAME(fc.parent_object_id, fc.parent_column_id) AS ColumnName,
        OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable,
        COL_NAME(fc.referenced_object_id, fc.referenced_column_id) AS ReferencedColumn
    FROM sys.foreign_keys AS fk
    INNER JOIN sys.foreign_key_columns AS fc ON fk.object_id = fc.constraint_object_id
    WHERE fk.parent_object_id = OBJECT_ID(N'[dbo].[FileDownloadLogs]');

    -- Show indexes
    PRINT ''
    PRINT 'Indexes:'
    SELECT
        i.name AS IndexName,
        i.type_desc AS IndexType,
        COL_NAME(ic.object_id, ic.column_id) AS ColumnName
    FROM sys.indexes AS i
    INNER JOIN sys.index_columns AS ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
    WHERE i.object_id = OBJECT_ID(N'[dbo].[FileDownloadLogs]')
    ORDER BY i.name, ic.index_column_id;

    PRINT ''
    PRINT '✓ Migration completed successfully!'
END
ELSE
BEGIN
    PRINT '✗ ERROR: Table [FileDownloadLogs] was not created!'
END

PRINT '================================================================================';
GO

/*
================================================================================
ROLLBACK SCRIPT (Use this to undo the migration)
================================================================================

-- WARNING: This will delete ALL download tracking data!
-- Only use this if you need to completely remove the feature

-- Drop indexes
DROP INDEX IF EXISTS [IX_FileId] ON [dbo].[FileDownloadLogs];
DROP INDEX IF EXISTS [IX_DownloadedByUserId] ON [dbo].[FileDownloadLogs];
DROP INDEX IF EXISTS [IX_DownloadedDate] ON [dbo].[FileDownloadLogs];

-- Drop foreign keys
ALTER TABLE [dbo].[FileDownloadLogs] DROP CONSTRAINT IF EXISTS [FK_dbo.FileDownloadLogs_dbo.SharedFiles_FileId];
ALTER TABLE [dbo].[FileDownloadLogs] DROP CONSTRAINT IF EXISTS [FK_dbo.FileDownloadLogs_dbo.Users_DownloadedByUserId];

-- Drop table
DROP TABLE IF EXISTS [dbo].[FileDownloadLogs];

-- Remove from migration history (if applicable)
DELETE FROM [dbo].[__MigrationHistory]
WHERE [MigrationId] = '202511111300000_AddFileDownloadTracking'
  AND [ContextKey] = 'FileSharePortal.Data.FileSharePortalContext';

PRINT 'Rollback completed.'

================================================================================
*/
