-- =============================================
-- Semester Historical Records: CORRECTED Migration
-- Uses correct database name: PortaliBITS
-- =============================================

USE [PortaliBITS]  -- CORRECTED DATABASE NAME
GO

BEGIN TRANSACTION;

PRINT '========================================';
PRINT 'SAFE MIGRATION - CORRECTED VERSION';
PRINT 'Database: PortaliBITS';
PRINT '========================================';
PRINT '';

-- =============================================
-- STEP 1: Check and Add SemesterId to Announcements (if table exists)
-- =============================================
PRINT 'STEP 1: Checking Announcements table...';

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Announcements')
BEGIN
    PRINT '  ✓ Announcements table exists';
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Announcements]') AND name = 'SemesterId')
    BEGIN
        ALTER TABLE [dbo].[Announcements] ADD [SemesterId] INT NULL;
        PRINT '  ✓ Added SemesterId to Announcements table';
    END
    ELSE
    BEGIN
        PRINT '  ℹ SemesterId already exists in Announcements table';
    END
    
    -- Add foreign key if needed
    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Announcements_Semester')
    BEGIN
        ALTER TABLE [dbo].[Announcements]
        ADD CONSTRAINT [FK_Announcements_Semester] 
        FOREIGN KEY ([SemesterId]) REFERENCES [dbo].[Semesters]([SemesterId])
        ON DELETE NO ACTION;
        PRINT '  ✓ Added FK_Announcements_Semester constraint';
    END
    
    -- Add index if needed
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Announcements_SemesterId')
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_Announcements_SemesterId] ON [dbo].[Announcements]([SemesterId]);
        PRINT '  ✓ Created IX_Announcements_SemesterId index';
    END
END
ELSE
BEGIN
    PRINT '  ⚠ Announcements table does NOT exist - SKIPPING';
END

PRINT '';

-- =============================================
-- STEP 2: Check and Add SemesterId to Remittances (if table exists)
-- =============================================
PRINT 'STEP 2: Checking Remittances table...';

IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Remittances')
BEGIN
    PRINT '  ✓ Remittances table exists';
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Remittances]') AND name = 'SemesterId')
    BEGIN
        ALTER TABLE [dbo].[Remittances] ADD [SemesterId] INT NULL;
        PRINT '  ✓ Added SemesterId to Remittances table';
    END
    ELSE
    BEGIN
        PRINT '  ℹ SemesterId already exists in Remittances table';
    END
    
    -- Add foreign key if needed
    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Remittances_Semester')
    BEGIN
        ALTER TABLE [dbo].[Remittances]
        ADD CONSTRAINT [FK_Remittances_Semester] 
        FOREIGN KEY ([SemesterId]) REFERENCES [dbo].[Semesters]([SemesterId])
        ON DELETE NO ACTION;
        PRINT '  ✓ Added FK_Remittances_Semester constraint';
    END
    
    -- Add index if needed
    IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Remittances_SemesterId')
    BEGIN
        CREATE NONCLUSTERED INDEX [IX_Remittances_SemesterId] ON [dbo].[Remittances]([SemesterId]);
        PRINT '  ✓ Created IX_Remittances_SemesterId index';
    END
END
ELSE
BEGIN
    PRINT '  ⚠ Remittances table does NOT exist - SKIPPING';
END

PRINT '';

-- =============================================
-- STEP 3: Create indexes for existing tables with SemesterId
-- =============================================
PRINT 'STEP 3: Creating indexes for tables that already have SemesterId...';
PRINT '';

-- Fees
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Fees')
BEGIN
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Fees]') AND name = 'SemesterId')
    BEGIN
        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Fees_SemesterId')
        BEGIN
            CREATE NONCLUSTERED INDEX [IX_Fees_SemesterId] ON [dbo].[Fees]([SemesterId]);
            PRINT '  ✓ Created IX_Fees_SemesterId index';
        END
        ELSE
        BEGIN
            PRINT '  ℹ IX_Fees_SemesterId already exists';
        END
    END
END

-- Fines
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Fines')
BEGIN
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Fines]') AND name = 'SemesterId')
    BEGIN
        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Fines_SemesterId')
        BEGIN
            CREATE NONCLUSTERED INDEX [IX_Fines_SemesterId] ON [dbo].[Fines]([SemesterId]);
            PRINT '  ✓ Created IX_Fines_SemesterId index';
        END
        ELSE
        BEGIN
            PRINT '  ℹ IX_Fines_SemesterId already exists';
        END
    END
END

-- Event
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Event')
BEGIN
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Event]') AND name = 'SemesterId')
    BEGIN
        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Event_SemesterId')
        BEGIN
            CREATE NONCLUSTERED INDEX [IX_Event_SemesterId] ON [dbo].[Event]([SemesterId]);
            PRINT '  ✓ Created IX_Event_SemesterId index';
        END
        ELSE
        BEGIN
            PRINT '  ℹ IX_Event_SemesterId already exists';
        END
    END
END

-- Attendance
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Attendance')
BEGIN
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Attendance]') AND name = 'SemesterId')
    BEGIN
        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Attendance_SemesterId')
        BEGIN
            CREATE NONCLUSTERED INDEX [IX_Attendance_SemesterId] ON [dbo].[Attendance]([SemesterId]);
            PRINT '  ✓ Created IX_Attendance_SemesterId index';
        END
        ELSE
        BEGIN
            PRINT '  ℹ IX_Attendance_SemesterId already exists';
        END
    END
END

-- PaymentTransactions
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PaymentTransactions')
BEGIN
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]') AND name = 'SemesterId')
    BEGIN
        IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PaymentTransactions_SemesterId')
        BEGIN
            CREATE NONCLUSTERED INDEX [IX_PaymentTransactions_SemesterId] ON [dbo].[PaymentTransactions]([SemesterId]);
            PRINT '  ✓ Created IX_PaymentTransactions_SemesterId index';
        END
        ELSE
        BEGIN
            PRINT '  ℹ IX_PaymentTransactions_SemesterId already exists';
        END
    END
END

-- =============================================
-- COMPLETION
-- =============================================
PRINT '';
PRINT '========================================';
PRINT 'MIGRATION COMPLETED SUCCESSFULLY!';
PRINT '========================================';
PRINT '';
PRINT 'Next Step: Run 02_Backfill_SemesterId_Data_FIXED.sql';
PRINT '========================================';

COMMIT TRANSACTION;
GO
