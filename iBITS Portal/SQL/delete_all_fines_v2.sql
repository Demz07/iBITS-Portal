-- ============================================================
-- Script to DELETE all fines and related records (Version 2)
-- WARNING: This will delete ALL fine data permanently!
-- ============================================================

BEGIN TRANSACTION;

BEGIN TRY
    -- Step 1: Delete RemittanceItems that reference Fines
    PRINT 'Step 1: Deleting RemittanceItems that reference Fines...';
    DELETE FROM [RemittanceItems] WHERE FineId IS NOT NULL;
    PRINT 'Deleted ' + CAST(@@ROWCOUNT AS VARCHAR) + ' RemittanceItems';

    -- Step 2: Delete FinePaymentTransactions (if this table exists)
    IF OBJECT_ID('dbo.FinePaymentTransactions', 'U') IS NOT NULL
    BEGIN
        PRINT 'Step 2: Deleting FinePaymentTransactions...';
        DELETE FROM [FinePaymentTransactions];
        PRINT 'Deleted ' + CAST(@@ROWCOUNT AS VARCHAR) + ' FinePaymentTransactions';
    END
    ELSE
    BEGIN
        PRINT 'Step 2: FinePaymentTransactions table does not exist, skipping...';
    END

    -- Step 3: Delete Remittances that are for Fines
    -- Use FineCategory column to identify fine remittances (safer approach)
    PRINT 'Step 3: Deleting Fine Remittances...';
    DELETE FROM [Remittances] WHERE FineCategory IS NOT NULL;
    PRINT 'Deleted ' + CAST(@@ROWCOUNT AS VARCHAR) + ' Fine Remittances';

    -- Step 4: Delete all Fines
    PRINT 'Step 4: Deleting all Fines...';
    DELETE FROM [Fines];
    PRINT 'Deleted ' + CAST(@@ROWCOUNT AS VARCHAR) + ' Fines';

    -- Step 5: RESET the ID counter to 0 (so the next one starts at 1)
    PRINT 'Step 5: Resetting Fines identity counter...';
    DBCC CHECKIDENT ('[Fines]', RESEED, 0);

    -- Step 6: Verify deletion
    PRINT '';
    PRINT '=== VERIFICATION RESULTS ===';
    
    DECLARE @RemainingFines INT;
    DECLARE @RemainingItems INT;
    DECLARE @RemainingRemittances INT;
    
    SELECT @RemainingFines = COUNT(*) FROM [Fines];
    SELECT @RemainingItems = COUNT(*) FROM [RemittanceItems] WHERE FineId IS NOT NULL;
    SELECT @RemainingRemittances = COUNT(*) FROM [Remittances] WHERE FineCategory IS NOT NULL;
    
    PRINT 'Remaining Fines: ' + CAST(@RemainingFines AS VARCHAR);
    PRINT 'Remaining Fine RemittanceItems: ' + CAST(@RemainingItems AS VARCHAR);
    PRINT 'Remaining Fine Remittances: ' + CAST(@RemainingRemittances AS VARCHAR);
    PRINT '';
    
    IF @RemainingFines = 0 AND @RemainingItems = 0 AND @RemainingRemittances = 0
    BEGIN
        PRINT '✅ SUCCESS: All fines and related records have been deleted successfully!';
        COMMIT TRANSACTION;
    END
    ELSE
    BEGIN
        PRINT '⚠️ WARNING: Some records may still remain. Check the counts above.';
        COMMIT TRANSACTION;
    END

END TRY
BEGIN CATCH
    PRINT '❌ ERROR: ' + ERROR_MESSAGE();
    ROLLBACK TRANSACTION;
END CATCH
