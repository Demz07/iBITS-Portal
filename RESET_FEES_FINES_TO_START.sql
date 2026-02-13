-- ============================================================
-- RESET FEES AND FINES TO START (Identity Seed = 1)
-- ============================================================
-- Purpose: Delete all fees and fines records and reset identity to 1
-- Date: February 12, 2026
-- WARNING: This will permanently delete all payment data!
-- ============================================================

USE PortaliBits;
GO

-- ============================================================
-- STEP 1: DISPLAY CURRENT COUNTS (Before Deletion)
-- ============================================================
PRINT '============================================================';
PRINT 'CURRENT RECORD COUNTS (Before Deletion)';
PRINT '============================================================';

SELECT 
    'Fees' AS TableName, 
    COUNT(*) AS RecordCount,
    ISNULL(MAX(FeeId), 0) AS MaxID
FROM Fees;

SELECT 
    'Fines' AS TableName, 
    COUNT(*) AS RecordCount,
    ISNULL(MAX(FineId), 0) AS MaxID
FROM Fines;

SELECT 
    'PaymentTransactions' AS TableName, 
    COUNT(*) AS RecordCount
FROM PaymentTransactions;

SELECT 
    'FinePaymentTransactions' AS TableName, 
    COUNT(*) AS RecordCount
FROM FinePaymentTransactions;

SELECT 
    'RemittanceItems' AS TableName, 
    COUNT(*) AS RecordCount
FROM RemittanceItems;

SELECT 
    'Remittances' AS TableName, 
    COUNT(*) AS RecordCount
FROM Remittances;

PRINT '';
PRINT '============================================================';
PRINT 'STARTING DELETION PROCESS...';
PRINT '============================================================';

BEGIN TRANSACTION;

BEGIN TRY
    -- ============================================================
    -- STEP 2: DELETE RELATED RECORDS (Cascading Delete)
    -- ============================================================
    
    PRINT 'Step 1: Deleting PaymentTransactions...';
    DELETE FROM PaymentTransactions;
    PRINT 'PaymentTransactions deleted: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
    
    PRINT 'Step 2: Deleting FinePaymentTransactions...';
    DELETE FROM FinePaymentTransactions;
    PRINT 'FinePaymentTransactions deleted: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
    
    PRINT 'Step 3: Deleting RemittanceItems...';
    DELETE FROM RemittanceItems;
    PRINT 'RemittanceItems deleted: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
    
    PRINT 'Step 4: Deleting Remittances...';
    DELETE FROM Remittances;
    PRINT 'Remittances deleted: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
    
    -- ============================================================
    -- STEP 3: DELETE NOTIFICATIONS RELATED TO FEES/FINES
    -- ============================================================
    
    PRINT 'Step 5: Deleting Fee/Fine related Notifications...';
    DELETE FROM Notifications 
    WHERE NotificationType IN ('Payment', 'Remittance', 'Fine');
    PRINT 'Notifications deleted: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
    
    -- ============================================================
    -- STEP 4: DELETE FEES AND FINES
    -- ============================================================
    
    PRINT 'Step 6: Deleting all Fees...';
    DELETE FROM Fees;
    PRINT 'Fees deleted: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
    
    PRINT 'Step 7: Deleting all Fines...';
    DELETE FROM Fines;
    PRINT 'Fines deleted: ' + CAST(@@ROWCOUNT AS VARCHAR(10));
    
    -- ============================================================
    -- STEP 5: RESET IDENTITY SEEDS TO 1
    -- ============================================================
    
    PRINT '';
    PRINT '============================================================';
    PRINT 'RESETTING IDENTITY SEEDS TO 1...';
    PRINT '============================================================';
    
    -- Check if tables have identity columns before resetting
    IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('Fees'))
    BEGIN
        DBCC CHECKIDENT ('Fees', RESEED, 0);
        PRINT 'Fees identity seed reset to 1';
    END
    
    IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('Fines'))
    BEGIN
        DBCC CHECKIDENT ('Fines', RESEED, 0);
        PRINT 'Fines identity seed reset to 1';
    END
    
    IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('PaymentTransactions'))
    BEGIN
        DBCC CHECKIDENT ('PaymentTransactions', RESEED, 0);
        PRINT 'PaymentTransactions identity seed reset to 1';
    END
    
    IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('FinePaymentTransactions'))
    BEGIN
        DBCC CHECKIDENT ('FinePaymentTransactions', RESEED, 0);
        PRINT 'FinePaymentTransactions identity seed reset to 1';
    END
    
    IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('Remittances'))
    BEGIN
        DBCC CHECKIDENT ('Remittances', RESEED, 0);
        PRINT 'Remittances identity seed reset to 1';
    END
    
    IF EXISTS (SELECT 1 FROM sys.identity_columns WHERE object_id = OBJECT_ID('RemittanceItems'))
    BEGIN
        DBCC CHECKIDENT ('RemittanceItems', RESEED, 0);
        PRINT 'RemittanceItems identity seed reset to 1';
    END
    
    -- ============================================================
    -- STEP 6: VERIFY DELETION
    -- ============================================================
    
    PRINT '';
    PRINT '============================================================';
    PRINT 'VERIFICATION - RECORD COUNTS (After Deletion)';
    PRINT '============================================================';
    
    DECLARE @FeesCount INT, @FinesCount INT, @PaymentTxnCount INT, 
            @FineTxnCount INT, @RemittanceCount INT, @RemittanceItemCount INT;
    
    SELECT @FeesCount = COUNT(*) FROM Fees;
    SELECT @FinesCount = COUNT(*) FROM Fines;
    SELECT @PaymentTxnCount = COUNT(*) FROM PaymentTransactions;
    SELECT @FineTxnCount = COUNT(*) FROM FinePaymentTransactions;
    SELECT @RemittanceCount = COUNT(*) FROM Remittances;
    SELECT @RemittanceItemCount = COUNT(*) FROM RemittanceItems;
    
    PRINT 'Fees remaining: ' + CAST(@FeesCount AS VARCHAR(10));
    PRINT 'Fines remaining: ' + CAST(@FinesCount AS VARCHAR(10));
    PRINT 'PaymentTransactions remaining: ' + CAST(@PaymentTxnCount AS VARCHAR(10));
    PRINT 'FinePaymentTransactions remaining: ' + CAST(@FineTxnCount AS VARCHAR(10));
    PRINT 'Remittances remaining: ' + CAST(@RemittanceCount AS VARCHAR(10));
    PRINT 'RemittanceItems remaining: ' + CAST(@RemittanceItemCount AS VARCHAR(10));
    
    -- Check if all tables are empty
    IF @FeesCount = 0 AND @FinesCount = 0 AND @PaymentTxnCount = 0 
       AND @FineTxnCount = 0 AND @RemittanceCount = 0 AND @RemittanceItemCount = 0
    BEGIN
        PRINT '';
        PRINT '✅ SUCCESS: All fees and fines data has been deleted!';
        PRINT '✅ Identity seeds reset to 1';
        PRINT '✅ Next Fee ID will be: 1';
        PRINT '✅ Next Fine ID will be: 1';
        
        -- COMMIT THE TRANSACTION
        COMMIT TRANSACTION;
        
        PRINT '';
        PRINT '============================================================';
        PRINT '🎉 RESET COMPLETE - READY FOR FRESH START!';
        PRINT '============================================================';
    END
    ELSE
    BEGIN
        PRINT '';
        PRINT '⚠️ WARNING: Some records may still exist!';
        PRINT 'Rolling back transaction...';
        ROLLBACK TRANSACTION;
    END
    
END TRY
BEGIN CATCH
    -- ============================================================
    -- ERROR HANDLING
    -- ============================================================
    
    PRINT '';
    PRINT '❌ ERROR OCCURRED! Rolling back transaction...';
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR(10));
    
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    
    -- Re-throw the error
    THROW;
END CATCH;

GO

-- ============================================================
-- VERIFICATION QUERIES (Run after script completes)
-- ============================================================

PRINT '';
PRINT '============================================================';
PRINT 'FINAL VERIFICATION - TABLE STATUS';
PRINT '============================================================';

-- Show current identity values
SELECT 
    OBJECT_NAME(object_id) AS TableName,
    name AS ColumnName,
    last_value AS CurrentIdentityValue,
    CAST(last_value AS INT) + CAST(increment_value AS INT) AS NextValue
FROM sys.identity_columns
WHERE OBJECT_NAME(object_id) IN ('Fees', 'Fines', 'PaymentTransactions', 
                                  'FinePaymentTransactions', 'Remittances', 'RemittanceItems')
ORDER BY TableName;

PRINT '';
PRINT 'Script execution completed!';
PRINT 'Date: ' + CONVERT(VARCHAR(25), GETDATE(), 120);

GO
