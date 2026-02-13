-- ============================================================
-- CHECK RESET STATUS - Verify Fees and Fines are deleted
-- ============================================================
-- Run this to check if the previous reset was successful
-- ============================================================

USE PortaliBits;
GO

PRINT '============================================================';
PRINT 'CHECKING CURRENT STATUS - Fees and Fines Tables';
PRINT '============================================================';
PRINT '';

-- Check record counts
SELECT 'Fees' AS TableName, COUNT(*) AS RecordCount FROM Fees
UNION ALL
SELECT 'Fines', COUNT(*) FROM Fines
UNION ALL
SELECT 'PaymentTransactions', COUNT(*) FROM PaymentTransactions
UNION ALL
SELECT 'FinePaymentTransactions', COUNT(*) FROM FinePaymentTransactions
UNION ALL
SELECT 'Remittances', COUNT(*) FROM Remittances
UNION ALL
SELECT 'RemittanceItems', COUNT(*) FROM RemittanceItems;

PRINT '';
PRINT '============================================================';
PRINT 'IDENTITY SEED STATUS - Next IDs';
PRINT '============================================================';
PRINT '';

-- Check current identity values
SELECT 
    OBJECT_NAME(object_id) AS TableName,
    name AS ColumnName,
    last_value AS CurrentSeed,
    CAST(last_value AS INT) + CAST(increment_value AS INT) AS NextID
FROM sys.identity_columns
WHERE OBJECT_NAME(object_id) IN ('Fees', 'Fines', 'PaymentTransactions', 
                                  'FinePaymentTransactions', 'Remittances', 'RemittanceItems')
ORDER BY TableName;

PRINT '';
PRINT '============================================================';
PRINT 'SUMMARY';
PRINT '============================================================';

DECLARE @FeesCount INT, @FinesCount INT;
SELECT @FeesCount = COUNT(*) FROM Fees;
SELECT @FinesCount = COUNT(*) FROM Fines;

IF @FeesCount = 0 AND @FinesCount = 0
BEGIN
    PRINT '✅ SUCCESS: Fees and Fines tables are empty!';
    PRINT '✅ Ready for fresh start!';
END
ELSE
BEGIN
    PRINT '⚠️ WARNING: Tables still contain data!';
    PRINT 'Fees: ' + CAST(@FeesCount AS VARCHAR(10));
    PRINT 'Fines: ' + CAST(@FinesCount AS VARCHAR(10));
    PRINT '';
    PRINT 'You may need to run RESET_FEES_FINES_FIXED.sql again.';
END

GO
