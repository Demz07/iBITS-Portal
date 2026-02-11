-- ============================================================
-- ROLLBACK SCRIPT: 010_ROLLBACK_RemittanceItems_For_Fees.sql
-- PURPOSE: Rollback the RemittanceItems backfill migration
-- ISSUE: Reverts changes made by 010_Backfill_RemittanceItems_For_Fees.sql
-- DATE: 2026-02-11
-- ============================================================

-- This script removes RemittanceItems that were created by the backfill migration
-- and restores the database to its pre-migration state

USE PortaliBits;
GO

SET NOCOUNT ON;
GO

PRINT '============================================================';
PRINT 'Starting RemittanceItems Backfill ROLLBACK';
PRINT 'Date: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================================';
PRINT '';

-- ============================================================
-- STEP 1: VERIFY BACKUP TABLES EXIST
-- ============================================================

PRINT '------------------------------------------------------------';
PRINT 'STEP 1: Checking for backup tables';
PRINT '------------------------------------------------------------';

IF OBJECT_ID('RemittanceItems_Backup_20260211', 'U') IS NULL
BEGIN
    PRINT 'ERROR: Backup table RemittanceItems_Backup_20260211 not found!';
    PRINT 'Cannot perform rollback without backup.';
    PRINT 'Aborting rollback operation.';
    RETURN;
END

IF OBJECT_ID('Fees_Backup_20260211', 'U') IS NULL
BEGIN
    PRINT 'ERROR: Backup table Fees_Backup_20260211 not found!';
    PRINT 'Cannot perform rollback without backup.';
    PRINT 'Aborting rollback operation.';
    RETURN;
END

PRINT 'OK: Backup tables found.';
PRINT '';

-- ============================================================
-- STEP 2: ANALYZE CURRENT STATE
-- ============================================================

PRINT '------------------------------------------------------------';
PRINT 'STEP 2: Analyzing current state';
PRINT '------------------------------------------------------------';

DECLARE @CurrentRemittanceItems INT;
SELECT @CurrentRemittanceItems = COUNT(*) FROM RemittanceItems;
PRINT 'Current RemittanceItems count: ' + CAST(@CurrentRemittanceItems AS VARCHAR);

DECLARE @BackupRemittanceItems INT;
SELECT @BackupRemittanceItems = COUNT(*) FROM RemittanceItems_Backup_20260211;
PRINT 'Backup RemittanceItems count: ' + CAST(@BackupRemittanceItems AS VARCHAR);

DECLARE @ItemsToDelete INT = @CurrentRemittanceItems - @BackupRemittanceItems;
PRINT 'Items to be deleted: ' + CAST(@ItemsToDelete AS VARCHAR);
PRINT '';

-- ============================================================
-- STEP 3: PERFORM ROLLBACK
-- ============================================================

PRINT '------------------------------------------------------------';
PRINT 'STEP 3: Performing rollback';
PRINT '------------------------------------------------------------';

BEGIN TRANSACTION;

BEGIN TRY
    -- Delete all RemittanceItems for fee remittances that were added after the backup
    DELETE FROM RemittanceItems
    WHERE RemittanceItemId NOT IN (SELECT RemittanceItemId FROM RemittanceItems_Backup_20260211);
    
    DECLARE @DeletedItems INT = @@ROWCOUNT;
    PRINT 'Deleted ' + CAST(@DeletedItems AS VARCHAR) + ' RemittanceItem records';

    -- Verify the deletion
    DECLARE @RemainingItems INT;
    SELECT @RemainingItems = COUNT(*) FROM RemittanceItems;
    PRINT 'Remaining RemittanceItems: ' + CAST(@RemainingItems AS VARCHAR);

    IF @RemainingItems = @BackupRemittanceItems
    BEGIN
        PRINT '';
        PRINT 'SUCCESS: RemittanceItems table restored to backup state!';
    END
    ELSE
    BEGIN
        PRINT '';
        PRINT 'WARNING: Count mismatch after rollback!';
        PRINT 'Expected: ' + CAST(@BackupRemittanceItems AS VARCHAR);
        PRINT 'Actual: ' + CAST(@RemainingItems AS VARCHAR);
    END

    COMMIT TRANSACTION;
    
    PRINT '';
    PRINT '============================================================';
    PRINT 'Rollback completed successfully!';
    PRINT '============================================================';
    PRINT '';
    PRINT 'Summary:';
    PRINT '  - RemittanceItems deleted: ' + CAST(@DeletedItems AS VARCHAR);
    PRINT '  - RemittanceItems remaining: ' + CAST(@RemainingItems AS VARCHAR);
    PRINT '';
    PRINT 'Note: Backup tables are still available for verification:';
    PRINT '  - RemittanceItems_Backup_20260211';
    PRINT '  - Fees_Backup_20260211';
    PRINT '';
    PRINT 'You can drop these tables manually if no longer needed.';
    PRINT '';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    
    PRINT '';
    PRINT '============================================================';
    PRINT 'ERROR: Rollback failed!';
    PRINT '============================================================';
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS VARCHAR);
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR);
    PRINT '';
    PRINT 'Transaction has been rolled back.';
    PRINT 'Database remains in current state.';
    PRINT '';
    
    -- Re-throw the error
    THROW;
END CATCH;

GO
