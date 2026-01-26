-- ============================================================
-- SQL SCRIPT: Rollback Remittance System Schema
-- iBITS Portal - UNDO all remittance changes
-- ============================================================
-- Run this script to completely remove all remittance-related
-- database objects and restore the original state.
-- ============================================================

USE [PortaliBits];
GO

PRINT '============================================================';
PRINT 'STARTING ROLLBACK - Remittance System';
PRINT '============================================================';
PRINT '';

-- ============================================================
-- STEP 1: Drop View
-- ============================================================
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_RemittanceSummary')
BEGIN
    DROP VIEW [dbo].[vw_RemittanceSummary];
    PRINT 'Dropped view: vw_RemittanceSummary';
END
GO

-- ============================================================
-- STEP 2: Drop Stored Procedure
-- ============================================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GenerateRemittanceBatchCode')
BEGIN
    DROP PROCEDURE [dbo].[sp_GenerateRemittanceBatchCode];
    PRINT 'Dropped stored procedure: sp_GenerateRemittanceBatchCode';
END
GO

-- ============================================================
-- STEP 3: Drop Indexes on Fees
-- ============================================================
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Fees_RemittanceStatus' AND object_id = OBJECT_ID('dbo.Fees'))
BEGIN
    DROP INDEX [IX_Fees_RemittanceStatus] ON [dbo].[Fees];
    PRINT 'Dropped index: IX_Fees_RemittanceStatus';
END
GO

-- ============================================================
-- STEP 4: Drop Indexes on Fines
-- ============================================================
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Fines_RemittanceStatus' AND object_id = OBJECT_ID('dbo.Fines'))
BEGIN
    DROP INDEX [IX_Fines_RemittanceStatus] ON [dbo].[Fines];
    PRINT 'Dropped index: IX_Fines_RemittanceStatus';
END
GO

-- ============================================================
-- STEP 5: Drop Indexes on Remittances
-- ============================================================
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Remittances_Status' AND object_id = OBJECT_ID('dbo.Remittances'))
BEGIN
    DROP INDEX [IX_Remittances_Status] ON [dbo].[Remittances];
    PRINT 'Dropped index: IX_Remittances_Status';
END
GO

IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Remittances_Section' AND object_id = OBJECT_ID('dbo.Remittances'))
BEGIN
    DROP INDEX [IX_Remittances_Section] ON [dbo].[Remittances];
    PRINT 'Dropped index: IX_Remittances_Section';
END
GO

-- ============================================================
-- STEP 6: Drop Indexes on RemittanceItems
-- ============================================================
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RemittanceItems_RemittanceId' AND object_id = OBJECT_ID('dbo.RemittanceItems'))
BEGIN
    DROP INDEX [IX_RemittanceItems_RemittanceId] ON [dbo].[RemittanceItems];
    PRINT 'Dropped index: IX_RemittanceItems_RemittanceId';
END
GO

-- ============================================================
-- STEP 7: Drop Foreign Keys and Constraints from Fees
-- ============================================================
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Fees_Remittances')
BEGIN
    ALTER TABLE [dbo].[Fees] DROP CONSTRAINT [FK_Fees_Remittances];
    PRINT 'Dropped FK: FK_Fees_Remittances';
END
GO

IF EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_Fees_RemittanceStatus')
BEGIN
    ALTER TABLE [dbo].[Fees] DROP CONSTRAINT [CK_Fees_RemittanceStatus];
    PRINT 'Dropped constraint: CK_Fees_RemittanceStatus';
END
GO

-- ============================================================
-- STEP 8: Drop Foreign Keys and Constraints from Fines
-- ============================================================
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Fines_Remittances')
BEGIN
    ALTER TABLE [dbo].[Fines] DROP CONSTRAINT [FK_Fines_Remittances];
    PRINT 'Dropped FK: FK_Fines_Remittances';
END
GO

IF EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_Fines_RemittanceStatus')
BEGIN
    ALTER TABLE [dbo].[Fines] DROP CONSTRAINT [CK_Fines_RemittanceStatus];
    PRINT 'Dropped constraint: CK_Fines_RemittanceStatus';
END
GO

-- ============================================================
-- STEP 9: Drop Columns from Fees
-- ============================================================
-- First, drop any default constraints on RemittanceStatus
DECLARE @ConstraintName NVARCHAR(256);
SELECT @ConstraintName = d.name
FROM sys.default_constraints d
JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
WHERE d.parent_object_id = OBJECT_ID('dbo.Fees') AND c.name = 'RemittanceStatus';

IF @ConstraintName IS NOT NULL
BEGIN
    EXEC('ALTER TABLE [dbo].[Fees] DROP CONSTRAINT [' + @ConstraintName + ']');
    PRINT 'Dropped default constraint: ' + @ConstraintName + ' on Fees.RemittanceStatus';
END
GO

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Fees') AND name = 'RemittanceStatus')
BEGIN
    ALTER TABLE [dbo].[Fees] DROP COLUMN [RemittanceStatus];
    PRINT 'Dropped column: Fees.RemittanceStatus';
END
GO

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Fees') AND name = 'RemittanceId')
BEGIN
    ALTER TABLE [dbo].[Fees] DROP COLUMN [RemittanceId];
    PRINT 'Dropped column: Fees.RemittanceId';
END
GO

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Fees') AND name = 'CollectedBy')
BEGIN
    ALTER TABLE [dbo].[Fees] DROP COLUMN [CollectedBy];
    PRINT 'Dropped column: Fees.CollectedBy';
END
GO

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Fees') AND name = 'CollectionDate')
BEGIN
    ALTER TABLE [dbo].[Fees] DROP COLUMN [CollectionDate];
    PRINT 'Dropped column: Fees.CollectionDate';
END
GO

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Fees') AND name = 'OfficialPaymentDate')
BEGIN
    ALTER TABLE [dbo].[Fees] DROP COLUMN [OfficialPaymentDate];
    PRINT 'Dropped column: Fees.OfficialPaymentDate';
END
GO

-- ============================================================
-- STEP 10: Drop Columns from Fines
-- ============================================================
-- First, drop any default constraints on RemittanceStatus
DECLARE @FinesConstraintName NVARCHAR(256);
SELECT @FinesConstraintName = d.name
FROM sys.default_constraints d
JOIN sys.columns c ON d.parent_object_id = c.object_id AND d.parent_column_id = c.column_id
WHERE d.parent_object_id = OBJECT_ID('dbo.Fines') AND c.name = 'RemittanceStatus';

IF @FinesConstraintName IS NOT NULL
BEGIN
    EXEC('ALTER TABLE [dbo].[Fines] DROP CONSTRAINT [' + @FinesConstraintName + ']');
    PRINT 'Dropped default constraint: ' + @FinesConstraintName + ' on Fines.RemittanceStatus';
END
GO

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Fines') AND name = 'RemittanceStatus')
BEGIN
    ALTER TABLE [dbo].[Fines] DROP COLUMN [RemittanceStatus];
    PRINT 'Dropped column: Fines.RemittanceStatus';
END
GO

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Fines') AND name = 'RemittanceId')
BEGIN
    ALTER TABLE [dbo].[Fines] DROP COLUMN [RemittanceId];
    PRINT 'Dropped column: Fines.RemittanceId';
END
GO

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Fines') AND name = 'CollectedBy')
BEGIN
    ALTER TABLE [dbo].[Fines] DROP COLUMN [CollectedBy];
    PRINT 'Dropped column: Fines.CollectedBy';
END
GO

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Fines') AND name = 'CollectionDate')
BEGIN
    ALTER TABLE [dbo].[Fines] DROP COLUMN [CollectionDate];
    PRINT 'Dropped column: Fines.CollectionDate';
END
GO

IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Fines') AND name = 'OfficialPaymentDate')
BEGIN
    ALTER TABLE [dbo].[Fines] DROP COLUMN [OfficialPaymentDate];
    PRINT 'Dropped column: Fines.OfficialPaymentDate';
END
GO

-- ============================================================
-- STEP 11: Drop RemittanceItems Table
-- ============================================================
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'RemittanceItems')
BEGIN
    DROP TABLE [dbo].[RemittanceItems];
    PRINT 'Dropped table: RemittanceItems';
END
GO

-- ============================================================
-- STEP 12: Drop Remittances Table
-- ============================================================
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Remittances')
BEGIN
    DROP TABLE [dbo].[Remittances];
    PRINT 'Dropped table: Remittances';
END
GO

-- ============================================================
-- VERIFICATION
-- ============================================================
PRINT '';
PRINT '============================================================';
PRINT 'ROLLBACK COMPLETE';
PRINT '============================================================';
PRINT '';

-- Verify tables are gone
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Remittances')
    PRINT '✓ Remittances table removed';
ELSE
    PRINT '✗ ERROR: Remittances table still exists!';

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RemittanceItems')
    PRINT '✓ RemittanceItems table removed';
ELSE
    PRINT '✗ ERROR: RemittanceItems table still exists!';

-- Verify columns are gone from Fees
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Fees') AND name = 'RemittanceStatus')
    PRINT '✓ Fees.RemittanceStatus column removed';
ELSE
    PRINT '✗ ERROR: Fees.RemittanceStatus column still exists!';

-- Verify columns are gone from Fines
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Fines') AND name = 'RemittanceStatus')
    PRINT '✓ Fines.RemittanceStatus column removed';
ELSE
    PRINT '✗ ERROR: Fines.RemittanceStatus column still exists!';

PRINT '';
PRINT 'Database has been restored to pre-remittance state.';
PRINT 'You can now run the fixed RemittanceSystem_Schema_v2.sql script.';
GO
