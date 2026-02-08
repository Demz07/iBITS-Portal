-- =============================================
-- ROLLBACK: Remove SemesterId columns and constraints
-- Run this if you need to undo the migration
-- =============================================

USE [iBITSPortal]
GO

BEGIN TRANSACTION;

PRINT '========================================';
PRINT 'ROLLING BACK SEMESTER MIGRATION';
PRINT '========================================';
PRINT '';

-- Drop indexes first
PRINT 'Dropping indexes...';

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Fees_SemesterId')
BEGIN
    DROP INDEX [IX_Fees_SemesterId] ON [dbo].[Fees];
    PRINT 'Dropped IX_Fees_SemesterId';
END

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Fines_SemesterId')
BEGIN
    DROP INDEX [IX_Fines_SemesterId] ON [dbo].[Fines];
    PRINT 'Dropped IX_Fines_SemesterId';
END

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Events_SemesterId')
BEGIN
    DROP INDEX [IX_Events_SemesterId] ON [dbo].[Events];
    PRINT 'Dropped IX_Events_SemesterId';
END

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Attendances_SemesterId')
BEGIN
    DROP INDEX [IX_Attendances_SemesterId] ON [dbo].[Attendances];
    PRINT 'Dropped IX_Attendances_SemesterId';
END

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_PaymentTransactions_SemesterId')
BEGIN
    DROP INDEX [IX_PaymentTransactions_SemesterId] ON [dbo].[PaymentTransactions];
    PRINT 'Dropped IX_PaymentTransactions_SemesterId';
END

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Announcements_SemesterId')
BEGIN
    DROP INDEX [IX_Announcements_SemesterId] ON [dbo].[Announcements];
    PRINT 'Dropped IX_Announcements_SemesterId';
END

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Remittances_SemesterId')
BEGIN
    DROP INDEX [IX_Remittances_SemesterId] ON [dbo].[Remittances];
    PRINT 'Dropped IX_Remittances_SemesterId';
END

PRINT '';
PRINT 'Dropping foreign key constraints...';

-- Drop foreign keys
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Announcements_Semester')
BEGIN
    ALTER TABLE [dbo].[Announcements] DROP CONSTRAINT [FK_Announcements_Semester];
    PRINT 'Dropped FK_Announcements_Semester';
END

IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Remittances_Semester')
BEGIN
    ALTER TABLE [dbo].[Remittances] DROP CONSTRAINT [FK_Remittances_Semester];
    PRINT 'Dropped FK_Remittances_Semester';
END

PRINT '';
PRINT 'Removing SemesterId columns...';

-- Drop columns
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Announcements]') AND name = 'SemesterId')
BEGIN
    ALTER TABLE [dbo].[Announcements] DROP COLUMN [SemesterId];
    PRINT 'Removed SemesterId from Announcements';
END

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Remittances]') AND name = 'SemesterId')
BEGIN
    ALTER TABLE [dbo].[Remittances] DROP COLUMN [SemesterId];
    PRINT 'Removed SemesterId from Remittances';
END

PRINT '';
PRINT '========================================';
PRINT 'ROLLBACK COMPLETED SUCCESSFULLY!';
PRINT '========================================';

COMMIT TRANSACTION;
GO
