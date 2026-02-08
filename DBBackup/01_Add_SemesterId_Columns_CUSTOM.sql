-- =============================================
-- Semester Historical Records: Custom Migration for Your Database
-- Script 1 of 3 - CUSTOM VERSION
-- Based on current database analysis
-- =============================================

USE [iBITSPortal]
GO

BEGIN TRANSACTION;

PRINT '========================================';
PRINT 'CUSTOM MIGRATION FOR YOUR DATABASE';
PRINT '========================================';
PRINT '';
PRINT 'Tables in your database:';
PRINT '  ✓ Fees - Already has SemesterId';
PRINT '  ✓ Fines - Already has SemesterId';
PRINT '  ✓ Event - Already has SemesterId';
PRINT '  ✓ Attendance - Already has SemesterId';
PRINT '  ✓ PaymentTransactions - Already has SemesterId';
PRINT '  ✗ Announcements - NEEDS SemesterId';
PRINT '  ✗ Remittances - NEEDS SemesterId';
PRINT '';
PRINT 'Starting migration...';
PRINT '';

-- =============================================
-- STEP 1: Add SemesterId to Announcements
-- =============================================
PRINT 'STEP 1: Adding SemesterId to Announcements...';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Announcements]') AND name = 'SemesterId')
BEGIN
    ALTER TABLE [dbo].[Announcements] ADD [SemesterId] INT NULL;
    PRINT '  ✓ Added SemesterId to Announcements table';
END
ELSE
BEGIN
    PRINT '  ℹ SemesterId already exists in Announcements table';
END

-- =============================================
-- STEP 2: Add SemesterId to Remittances
-- =============================================
PRINT '';
PRINT 'STEP 2: Adding SemesterId to Remittances...';

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Remittances]') AND name = 'SemesterId')
BEGIN
    ALTER TABLE [dbo].[Remittances] ADD [SemesterId] INT NULL;
    PRINT '  ✓ Added SemesterId to Remittances table';
END
ELSE
BEGIN
    PRINT '  ℹ SemesterId already exists in Remittances table';
END

-- =============================================
-- STEP 3: Add Foreign Key Constraints
-- =============================================
PRINT '';
PRINT 'STEP 3: Adding foreign key constraints...';

-- Announcements FK
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Announcements_Semester')
BEGIN
    ALTER TABLE [dbo].[Announcements]
    ADD CONSTRAINT [FK_Announcements_Semester] 
    FOREIGN KEY ([SemesterId]) REFERENCES [dbo].[Semesters]([SemesterId])
    ON DELETE NO ACTION;
    PRINT '  ✓ Added FK_Announcements_Semester constraint';
END
ELSE
BEGIN
    PRINT '  ℹ FK_Announcements_Semester already exists';
END

-- Remittances FK
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Remittances_Semester')
BEGIN
    ALTER TABLE [dbo].[Remittances]
    ADD CONSTRAINT [FK_Remittances_Semester] 
    FOREIGN KEY ([SemesterId]) REFERENCES [dbo].[Semesters]([SemesterId])
    ON DELETE NO ACTION;
    PRINT '  ✓ Added FK_Remittances_Semester constraint';
END
ELSE
BEGIN
    PRINT '  ℹ FK_Remittances_Semester already exists';
END

-- =============================================
-- STEP 4: Create Indexes for Performance
-- =============================================
PRINT '';
PRINT 'STEP 4: Creating indexes for performance...';
PRINT '';

-- Index for Fees (already has SemesterId column)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Fees_SemesterId' AND object_id = OBJECT_ID(N'[dbo].[Fees]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Fees_SemesterId] ON [dbo].[Fees]([SemesterId]);
    PRINT '  ✓ Created IX_Fees_SemesterId index';
END
ELSE
BEGIN
    PRINT '  ℹ IX_Fees_SemesterId already exists';
END

-- Index for Fines (already has SemesterId column)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Fines_SemesterId' AND object_id = OBJECT_ID(N'[dbo].[Fines]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Fines_SemesterId] ON [dbo].[Fines]([SemesterId]);
    PRINT '  ✓ Created IX_Fines_SemesterId index';
END
ELSE
BEGIN
    PRINT '  ℹ IX_Fines_SemesterId already exists';
END

-- Index for Event (already has SemesterId column)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Event_SemesterId' AND object_id = OBJECT_ID(N'[dbo].[Event]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Event_SemesterId] ON [dbo].[Event]([SemesterId]);
    PRINT '  ✓ Created IX_Event_SemesterId index';
END
ELSE
BEGIN
    PRINT '  ℹ IX_Event_SemesterId already exists';
END

-- Index for Attendance (already has SemesterId column)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Attendance_SemesterId' AND object_id = OBJECT_ID(N'[dbo].[Attendance]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Attendance_SemesterId] ON [dbo].[Attendance]([SemesterId]);
    PRINT '  ✓ Created IX_Attendance_SemesterId index';
END
ELSE
BEGIN
    PRINT '  ℹ IX_Attendance_SemesterId already exists';
END

-- Index for PaymentTransactions (already has SemesterId column)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PaymentTransactions_SemesterId' AND object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_PaymentTransactions_SemesterId] ON [dbo].[PaymentTransactions]([SemesterId]);
    PRINT '  ✓ Created IX_PaymentTransactions_SemesterId index';
END
ELSE
BEGIN
    PRINT '  ℹ IX_PaymentTransactions_SemesterId already exists';
END

-- Index for Announcements (just added SemesterId)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Announcements_SemesterId' AND object_id = OBJECT_ID(N'[dbo].[Announcements]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Announcements_SemesterId] ON [dbo].[Announcements]([SemesterId]);
    PRINT '  ✓ Created IX_Announcements_SemesterId index';
END
ELSE
BEGIN
    PRINT '  ℹ IX_Announcements_SemesterId already exists';
END

-- Index for Remittances (just added SemesterId)
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Remittances_SemesterId' AND object_id = OBJECT_ID(N'[dbo].[Remittances]'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Remittances_SemesterId] ON [dbo].[Remittances]([SemesterId]);
    PRINT '  ✓ Created IX_Remittances_SemesterId index';
END
ELSE
BEGIN
    PRINT '  ℹ IX_Remittances_SemesterId already exists';
END

-- =============================================
-- COMPLETION
-- =============================================
PRINT '';
PRINT '========================================';
PRINT 'MIGRATION COMPLETED SUCCESSFULLY!';
PRINT '========================================';
PRINT '';
PRINT 'Summary:';
PRINT '  - SemesterId columns added: 2 (Announcements, Remittances)';
PRINT '  - Foreign keys created: 2';
PRINT '  - Indexes created: 7 (for all tables with SemesterId)';
PRINT '';
PRINT 'Next Step: Run 02_Backfill_SemesterId_Data.sql';
PRINT '========================================';

COMMIT TRANSACTION;
GO
