-- =============================================
-- Semester Historical Records: Add SemesterId Columns (FIXED)
-- Script 1 of 3 - FIXED VERSION
-- Checks if tables exist before creating indexes
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
PRINT '';

-- Create indexes only if tables exist
-- Check and create index for Fees
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Fees')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Fees_SemesterId')
    BEGIN
        IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Fees]') AND name = 'SemesterId')
        BEGIN
            CREATE INDEX [IX_Fees_SemesterId] ON [dbo].[Fees]([SemesterId]);
            PRINT 'Created IX_Fees_SemesterId index';
        END
        ELSE
        BEGIN
            PRINT 'Skipped IX_Fees_SemesterId - SemesterId column does not exist';
        END
    END
    ELSE
    BEGIN
        PRINT 'IX_Fees_SemesterId already exists';
    END
END
ELSE
BEGIN
    PRINT 'Skipped IX_Fees_SemesterId - Fees table does not exist';
END

-- Check and create index for Fines
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Fines')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Fines_SemesterId')
    BEGIN
        IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Fines]') AND name = 'SemesterId')
        BEGIN
            CREATE INDEX [IX_Fines_SemesterId] ON [dbo].[Fines]([SemesterId]);
            PRINT 'Created IX_Fines_SemesterId index';
        END
        ELSE
        BEGIN
            PRINT 'Skipped IX_Fines_SemesterId - SemesterId column does not exist';
        END
    END
    ELSE
    BEGIN
        PRINT 'IX_Fines_SemesterId already exists';
    END
END
ELSE
BEGIN
    PRINT 'Skipped IX_Fines_SemesterId - Fines table does not exist';
END

-- Check and create index for Events
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Events')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Events_SemesterId')
    BEGIN
        IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Events]') AND name = 'SemesterId')
        BEGIN
            CREATE INDEX [IX_Events_SemesterId] ON [dbo].[Events]([SemesterId]);
            PRINT 'Created IX_Events_SemesterId index';
        END
        ELSE
        BEGIN
            PRINT 'Skipped IX_Events_SemesterId - SemesterId column does not exist';
        END
    END
    ELSE
    BEGIN
        PRINT 'IX_Events_SemesterId already exists';
    END
END
ELSE
BEGIN
    PRINT 'Skipped IX_Events_SemesterId - Events table does not exist';
END

-- Check and create index for Attendances (This was causing the error)
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Attendances')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Attendances_SemesterId')
    BEGIN
        IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Attendances]') AND name = 'SemesterId')
        BEGIN
            CREATE INDEX [IX_Attendances_SemesterId] ON [dbo].[Attendances]([SemesterId]);
            PRINT 'Created IX_Attendances_SemesterId index';
        END
        ELSE
        BEGIN
            PRINT 'Skipped IX_Attendances_SemesterId - SemesterId column does not exist';
        END
    END
    ELSE
    BEGIN
        PRINT 'IX_Attendances_SemesterId already exists';
    END
END
ELSE
BEGIN
    PRINT 'Skipped IX_Attendances_SemesterId - Attendances table does not exist';
END

-- Check and create index for PaymentTransactions
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PaymentTransactions')
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PaymentTransactions_SemesterId')
    BEGIN
        IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]') AND name = 'SemesterId')
        BEGIN
            CREATE INDEX [IX_PaymentTransactions_SemesterId] ON [dbo].[PaymentTransactions]([SemesterId]);
            PRINT 'Created IX_PaymentTransactions_SemesterId index';
        END
        ELSE
        BEGIN
            PRINT 'Skipped IX_PaymentTransactions_SemesterId - SemesterId column does not exist';
        END
    END
    ELSE
    BEGIN
        PRINT 'IX_PaymentTransactions_SemesterId already exists';
    END
END
ELSE
BEGIN
    PRINT 'Skipped IX_PaymentTransactions_SemesterId - PaymentTransactions table does not exist';
END

-- Check and create index for Announcements
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Announcements_SemesterId')
BEGIN
    CREATE INDEX [IX_Announcements_SemesterId] ON [dbo].[Announcements]([SemesterId]);
    PRINT 'Created IX_Announcements_SemesterId index';
END
ELSE
BEGIN
    PRINT 'IX_Announcements_SemesterId already exists';
END

-- Check and create index for Remittances
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Remittances_SemesterId')
BEGIN
    CREATE INDEX [IX_Remittances_SemesterId] ON [dbo].[Remittances]([SemesterId]);
    PRINT 'Created IX_Remittances_SemesterId index';
END
ELSE
BEGIN
    PRINT 'IX_Remittances_SemesterId already exists';
END

PRINT '';
PRINT '========================================';
PRINT 'Script 1 completed successfully!';
PRINT 'Next: Run 02_Backfill_SemesterId_Data.sql';
PRINT '========================================';

COMMIT TRANSACTION;
GO
