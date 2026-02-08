-- =============================================
-- Semester Historical Records: Add SemesterId Columns
-- Script 1 of 3
-- =============================================

USE [iBITSPortal]
GO

BEGIN TRANSACTION;

PRINT 'Adding SemesterId columns to tables...';
PRINT '';

-- Add SemesterId to Announcements (if not exists)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Announcements]') AND name = 'SemesterId')
BEGIN
    ALTER TABLE [dbo].[Announcements] ADD [SemesterId] INT NULL;
    PRINT 'Added SemesterId to Announcements table';
END
ELSE
BEGIN
    PRINT 'SemesterId already exists in Announcements table';
END

-- Add SemesterId to Remittances (if not exists)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Remittances]') AND name = 'SemesterId')
BEGIN
    ALTER TABLE [dbo].[Remittances] ADD [SemesterId] INT NULL;
    PRINT 'Added SemesterId to Remittances table';
END
ELSE
BEGIN
    PRINT 'SemesterId already exists in Remittances table';
END

PRINT '';
PRINT 'Adding foreign key constraints...';

-- Add foreign key for Announcements (if not exists)
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Announcements_Semester')
BEGIN
    ALTER TABLE [dbo].[Announcements]
    ADD CONSTRAINT [FK_Announcements_Semester] 
    FOREIGN KEY ([SemesterId]) REFERENCES [dbo].[Semesters]([SemesterId])
    ON DELETE NO ACTION;
    PRINT 'Added FK_Announcements_Semester constraint';
END
ELSE
BEGIN
    PRINT 'FK_Announcements_Semester already exists';
END

-- Add foreign key for Remittances (if not exists)
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Remittances_Semester')
BEGIN
    ALTER TABLE [dbo].[Remittances]
    ADD CONSTRAINT [FK_Remittances_Semester] 
    FOREIGN KEY ([SemesterId]) REFERENCES [dbo].[Semesters]([SemesterId])
    ON DELETE NO ACTION;
    PRINT 'Added FK_Remittances_Semester constraint';
END
ELSE
BEGIN
    PRINT 'FK_Remittances_Semester already exists';
END

PRINT '';
PRINT 'Creating indexes for performance...';

-- Create indexes for better query performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Fees_SemesterId')
BEGIN
    CREATE INDEX [IX_Fees_SemesterId] ON [dbo].[Fees]([SemesterId]);
    PRINT 'Created IX_Fees_SemesterId index';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Fines_SemesterId')
BEGIN
    CREATE INDEX [IX_Fines_SemesterId] ON [dbo].[Fines]([SemesterId]);
    PRINT 'Created IX_Fines_SemesterId index';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Events_SemesterId')
BEGIN
    CREATE INDEX [IX_Events_SemesterId] ON [dbo].[Events]([SemesterId]);
    PRINT 'Created IX_Events_SemesterId index';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Attendances_SemesterId')
BEGIN
    CREATE INDEX [IX_Attendances_SemesterId] ON [dbo].[Attendances]([SemesterId]);
    PRINT 'Created IX_Attendances_SemesterId index';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PaymentTransactions_SemesterId')
BEGIN
    CREATE INDEX [IX_PaymentTransactions_SemesterId] ON [dbo].[PaymentTransactions]([SemesterId]);
    PRINT 'Created IX_PaymentTransactions_SemesterId index';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Announcements_SemesterId')
BEGIN
    CREATE INDEX [IX_Announcements_SemesterId] ON [dbo].[Announcements]([SemesterId]);
    PRINT 'Created IX_Announcements_SemesterId index';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Remittances_SemesterId')
BEGIN
    CREATE INDEX [IX_Remittances_SemesterId] ON [dbo].[Remittances]([SemesterId]);
    PRINT 'Created IX_Remittances_SemesterId index';
END

PRINT '';
PRINT '========================================';
PRINT 'Script 1 completed successfully!';
PRINT 'Next: Run 02_Backfill_SemesterId_Data.sql';
PRINT '========================================';

COMMIT TRANSACTION;
GO
