-- =============================================
-- File Share Portal API Tables Migration
-- Adds support for API authentication and application logging
-- =============================================

USE [FileSharePortal]
GO

-- =============================================
-- 1. Update Applications table to add API-related fields
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Applications]') AND name = 'ApiKey')
BEGIN
    ALTER TABLE [dbo].[Applications]
    ADD [ApiKey] NVARCHAR(500) NULL;
    PRINT 'Added ApiKey column to Applications table';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Applications]') AND name = 'RegisteredByUserId')
BEGIN
    ALTER TABLE [dbo].[Applications]
    ADD [RegisteredByUserId] INT NULL;
    PRINT 'Added RegisteredByUserId column to Applications table';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Applications]') AND name = 'RegisteredDate')
BEGIN
    ALTER TABLE [dbo].[Applications]
    ADD [RegisteredDate] DATETIME NULL;
    PRINT 'Added RegisteredDate column to Applications table';
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Applications]') AND name = 'ContactEmail')
BEGIN
    ALTER TABLE [dbo].[Applications]
    ADD [ContactEmail] NVARCHAR(200) NULL;
    PRINT 'Added ContactEmail column to Applications table';
END
GO

-- =============================================
-- 2. Update ApplicationExecutions table to add new fields
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[ApplicationExecutions]') AND name = 'ExecutedByUserId')
BEGIN
    ALTER TABLE [dbo].[ApplicationExecutions]
    ADD [ExecutedByUserId] INT NULL;
    PRINT 'Added ExecutedByUserId column to ApplicationExecutions table';
END
GO

-- =============================================
-- 3. Create ApiTokens table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ApiTokens]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ApiTokens] (
        [TokenId] INT IDENTITY(1,1) PRIMARY KEY,
        [Token] NVARCHAR(500) NOT NULL,
        [UserId] INT NOT NULL,
        [ApplicationId] INT NULL,
        [CreatedDate] DATETIME NOT NULL DEFAULT GETDATE(),
        [ExpiresDate] DATETIME NOT NULL,
        [IsRevoked] BIT NOT NULL DEFAULT 0,
        [LastUsedDate] DATETIME NULL,
        [IpAddress] NVARCHAR(100) NULL,
        CONSTRAINT [FK_ApiTokens_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([UserId]),
        CONSTRAINT [FK_ApiTokens_Applications] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications]([ApplicationId])
    );

    -- Add index on Token for fast lookup
    CREATE NONCLUSTERED INDEX [IX_ApiTokens_Token] ON [dbo].[ApiTokens]([Token]);

    -- Add index on UserId
    CREATE NONCLUSTERED INDEX [IX_ApiTokens_UserId] ON [dbo].[ApiTokens]([UserId]);

    PRINT 'Created ApiTokens table';
END
GO

-- =============================================
-- 4. Create ApplicationLogFiles table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ApplicationLogFiles]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ApplicationLogFiles] (
        [LogFileId] INT IDENTITY(1,1) PRIMARY KEY,
        [ExecutionId] INT NOT NULL,
        [FileName] NVARCHAR(255) NOT NULL,
        [ContentType] NVARCHAR(100) NOT NULL,
        [FileSize] BIGINT NOT NULL,
        [FileContent] VARBINARY(MAX) NOT NULL,
        [UploadedDate] DATETIME NOT NULL DEFAULT GETDATE(),
        [Description] NVARCHAR(500) NULL,
        CONSTRAINT [FK_ApplicationLogFiles_ApplicationExecutions] FOREIGN KEY ([ExecutionId]) REFERENCES [dbo].[ApplicationExecutions]([ExecutionId]) ON DELETE CASCADE
    );

    -- Add index on ExecutionId
    CREATE NONCLUSTERED INDEX [IX_ApplicationLogFiles_ExecutionId] ON [dbo].[ApplicationLogFiles]([ExecutionId]);

    PRINT 'Created ApplicationLogFiles table';
END
GO

-- =============================================
-- 5. Add foreign key constraints for new columns
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Applications_RegisteredByUser]') AND parent_object_id = OBJECT_ID(N'[dbo].[Applications]'))
BEGIN
    ALTER TABLE [dbo].[Applications]
    ADD CONSTRAINT [FK_Applications_RegisteredByUser] FOREIGN KEY ([RegisteredByUserId]) REFERENCES [dbo].[Users]([UserId]);
    PRINT 'Added FK_Applications_RegisteredByUser constraint';
END
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_ApplicationExecutions_ExecutedByUser]') AND parent_object_id = OBJECT_ID(N'[dbo].[ApplicationExecutions]'))
BEGIN
    ALTER TABLE [dbo].[ApplicationExecutions]
    ADD CONSTRAINT [FK_ApplicationExecutions_ExecutedByUser] FOREIGN KEY ([ExecutedByUserId]) REFERENCES [dbo].[Users]([UserId]);
    PRINT 'Added FK_ApplicationExecutions_ExecutedByUser constraint';
END
GO

PRINT 'API tables migration completed successfully!';
GO
