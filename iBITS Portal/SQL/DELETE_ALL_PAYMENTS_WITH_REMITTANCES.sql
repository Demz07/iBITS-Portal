-- ============================================================
-- DELETE ALL PAYMENTS WITH REMITTANCES SCRIPT
-- iBITS Portal - Complete Payment Data Reset
-- ============================================================
-- ⚠️ WARNING: This script will DELETE ALL records from:
--    1. RemittanceItems (child records first)
--    2. Remittances (batch remittance records)
--    3. Fees (all fee/payment records)
--    4. Fines (all fine records)
--    5. PaymentTransactions (if exists)
-- ============================================================
-- ⚠️ This action CANNOT be undone!
-- ⚠️ Make sure to backup your database before running this script!
-- ============================================================

USE [PortaliBITS];  -- Replace with your actual database name if different
GO

-- ============================================================
-- SAFETY CHECK: Display current record counts
-- ============================================================
PRINT '============================================================';
PRINT 'CURRENT DATABASE STATUS - BEFORE DELETION';
PRINT '============================================================';
PRINT '';

-- Count Remittances
DECLARE @RemittanceCount INT = (SELECT COUNT(*) FROM [dbo].[Remittances]);
DECLARE @RemittanceItemCount INT = (SELECT COUNT(*) FROM [dbo].[RemittanceItems]);
DECLARE @FeeCount INT = (SELECT COUNT(*) FROM [dbo].[Fees]);
DECLARE @FineCount INT = (SELECT COUNT(*) FROM [dbo].[Fines]);
DECLARE @PaymentTxCount INT = 0;

-- Check if PaymentTransactions table exists
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PaymentTransactions')
BEGIN
    SET @PaymentTxCount = (SELECT COUNT(*) FROM [dbo].[PaymentTransactions]);
END

PRINT 'Remittances:        ' + CAST(@RemittanceCount AS VARCHAR(10)) + ' records';
PRINT 'RemittanceItems:    ' + CAST(@RemittanceItemCount AS VARCHAR(10)) + ' records';
PRINT 'Fees:               ' + CAST(@FeeCount AS VARCHAR(10)) + ' records';
PRINT 'Fines:              ' + CAST(@FineCount AS VARCHAR(10)) + ' records';
PRINT 'PaymentTransactions: ' + CAST(@PaymentTxCount AS VARCHAR(10)) + ' records';
PRINT '';

-- Detailed breakdown
PRINT '--- Fees Breakdown ---';
SELECT 
    COUNT(*) AS TotalFees,
    SUM(CASE WHEN FeeStatus = 'Unpaid' OR FeeStatus = 'Pending' THEN 1 ELSE 0 END) AS UnpaidFees,
    SUM(CASE WHEN FeeStatus = 'Paid' OR FeeStatus = 'Completed' THEN 1 ELSE 0 END) AS PaidFees,
    SUM(CASE WHEN RemittanceStatus = 'NotRemitted' THEN 1 ELSE 0 END) AS NotRemitted,
    SUM(CASE WHEN RemittanceStatus = 'PendingRemittance' THEN 1 ELSE 0 END) AS PendingRemittance,
    SUM(CASE WHEN RemittanceStatus = 'Remitted' THEN 1 ELSE 0 END) AS Remitted,
    ISNULL(SUM(Amount), 0) AS TotalAmount
FROM [dbo].[Fees];

PRINT '';
PRINT '--- Fines Breakdown ---';
SELECT 
    COUNT(*) AS TotalFines,
    SUM(CASE WHEN FinesStatus = 'Unpaid' OR FinesStatus = 'Pending' THEN 1 ELSE 0 END) AS UnpaidFines,
    SUM(CASE WHEN FinesStatus = 'Paid' OR FinesStatus = 'Completed' THEN 1 ELSE 0 END) AS PaidFines,
    SUM(CASE WHEN RemittanceStatus = 'NotRemitted' THEN 1 ELSE 0 END) AS NotRemitted,
    SUM(CASE WHEN RemittanceStatus = 'PendingRemittance' THEN 1 ELSE 0 END) AS PendingRemittance,
    SUM(CASE WHEN RemittanceStatus = 'Remitted' THEN 1 ELSE 0 END) AS Remitted,
    ISNULL(SUM(Amount), 0) AS TotalAmount
FROM [dbo].[Fines];

PRINT '';
PRINT '--- Remittances Breakdown ---';
SELECT 
    COUNT(*) AS TotalRemittances,
    SUM(CASE WHEN Status = 'Pending' THEN 1 ELSE 0 END) AS Pending,
    SUM(CASE WHEN Status = 'Validated' THEN 1 ELSE 0 END) AS Validated,
    SUM(CASE WHEN Status = 'Rejected' THEN 1 ELSE 0 END) AS Rejected,
    ISNULL(SUM(TotalAmount), 0) AS TotalAmount
FROM [dbo].[Remittances];

PRINT '';
PRINT '⚠️ WARNING: You are about to delete ALL payment data from the database!';
PRINT '⚠️ This includes all fees, fines, remittances, and related records!';
PRINT '';
PRINT '============================================================';
GO

-- ============================================================
-- OPTION 1: DELETE ALL DATA (COMPLETE RESET)
-- ============================================================
-- ⚠️ UNCOMMENT THE SECTION BELOW TO EXECUTE
-- ============================================================

BEGIN TRANSACTION;
BEGIN TRY
    PRINT '';
    PRINT '============================================================';
    PRINT 'STARTING DELETION PROCESS...';
    PRINT '============================================================';
    
    -- Step 1: Delete PaymentTransactions (if exists)
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PaymentTransactions')
    BEGIN
        DELETE FROM [dbo].[PaymentTransactions];
        PRINT 'Step 1: Deleted PaymentTransactions - ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' rows';
    END
    ELSE
    BEGIN
        PRINT 'Step 1: PaymentTransactions table does not exist - skipping';
    END
    
    -- Step 2: Clear RemittanceId references from Fees (break FK before deleting remittances)
    UPDATE [dbo].[Fees] SET RemittanceId = NULL WHERE RemittanceId IS NOT NULL;
    PRINT 'Step 2: Cleared RemittanceId from Fees - ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' rows updated';
    
    -- Step 3: Clear RemittanceId references from Fines
    UPDATE [dbo].[Fines] SET RemittanceId = NULL WHERE RemittanceId IS NOT NULL;
    PRINT 'Step 3: Cleared RemittanceId from Fines - ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' rows updated';
    
    -- Step 4: Delete RemittanceItems (child records - will cascade but let's be explicit)
    DELETE FROM [dbo].[RemittanceItems];
    PRINT 'Step 4: Deleted RemittanceItems - ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' rows';
    
    -- Step 5: Delete Remittances (parent remittance records)
    DELETE FROM [dbo].[Remittances];
    PRINT 'Step 5: Deleted Remittances - ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' rows';
    
    -- Step 6: Delete all Fees
    DELETE FROM [dbo].[Fees];
    PRINT 'Step 6: Deleted Fees - ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' rows';
    
    -- Step 7: Delete all Fines
    DELETE FROM [dbo].[Fines];
    PRINT 'Step 7: Deleted Fines - ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' rows';
    
    PRINT '';
    PRINT '============================================================';
    PRINT 'ALL DELETIONS COMPLETED SUCCESSFULLY!';
    PRINT '============================================================';
    
    COMMIT TRANSACTION;
    PRINT 'Transaction committed.';
    
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT '';
    PRINT '============================================================';
    PRINT 'ERROR OCCURRED - TRANSACTION ROLLED BACK!';
    PRINT '============================================================';
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS VARCHAR(10));
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR(10));
    PRINT '============================================================';
END CATCH
GO

-- ============================================================
-- RESET IDENTITY SEEDS
-- ============================================================
PRINT '';
PRINT '============================================================';
PRINT 'RESETTING IDENTITY SEEDS...';
PRINT '============================================================';

-- Reset Fees identity
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Fees')
BEGIN
    DBCC CHECKIDENT ('[Fees]', RESEED, 0);
    PRINT 'Reset identity: Fees';
END

-- Reset Fines identity
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Fines')
BEGIN
    DBCC CHECKIDENT ('[Fines]', RESEED, 0);
    PRINT 'Reset identity: Fines';
END

-- Reset Remittances identity
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Remittances')
BEGIN
    DBCC CHECKIDENT ('[Remittances]', RESEED, 0);
    PRINT 'Reset identity: Remittances';
END

-- Reset RemittanceItems identity
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'RemittanceItems')
BEGIN
    DBCC CHECKIDENT ('[RemittanceItems]', RESEED, 0);
    PRINT 'Reset identity: RemittanceItems';
END

-- Reset PaymentTransactions identity (if exists)
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PaymentTransactions')
BEGIN
    DBCC CHECKIDENT ('[PaymentTransactions]', RESEED, 0);
    PRINT 'Reset identity: PaymentTransactions';
END

GO

-- ============================================================
-- VERIFICATION: Confirm all data was deleted
-- ============================================================
PRINT '';
PRINT '============================================================';
PRINT 'VERIFICATION - AFTER DELETION';
PRINT '============================================================';

SELECT 'Remittances' AS TableName, COUNT(*) AS RecordCount FROM [dbo].[Remittances]
UNION ALL
SELECT 'RemittanceItems', COUNT(*) FROM [dbo].[RemittanceItems]
UNION ALL
SELECT 'Fees', COUNT(*) FROM [dbo].[Fees]
UNION ALL
SELECT 'Fines', COUNT(*) FROM [dbo].[Fines];

-- Check PaymentTransactions if exists
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PaymentTransactions')
BEGIN
    SELECT 'PaymentTransactions' AS TableName, COUNT(*) AS RecordCount FROM [dbo].[PaymentTransactions];
END

PRINT '';
PRINT '============================================================';
PRINT 'DELETION SCRIPT COMPLETED';
PRINT '============================================================';
PRINT '';
PRINT 'All payment data has been deleted and identity seeds reset.';
PRINT 'You can now add fresh fee/fine records.';
PRINT '============================================================';
GO

-- ============================================================
-- OPTIONAL: DELETE ONLY FEES (Keep Fines)
-- ============================================================
-- Uncomment and run this section if you only want to delete fees
-- ============================================================
/*
BEGIN TRANSACTION;
BEGIN TRY
    -- Clear RemittanceId from Fees
    UPDATE [dbo].[Fees] SET RemittanceId = NULL WHERE RemittanceId IS NOT NULL;
    
    -- Delete RemittanceItems for Fees only
    DELETE FROM [dbo].[RemittanceItems] WHERE FeeId IS NOT NULL;
    
    -- Delete Fee-type Remittances only
    DELETE FROM [dbo].[Remittances] WHERE RemittanceType = 'Fee';
    
    -- Delete all Fees
    DELETE FROM [dbo].[Fees];
    
    COMMIT TRANSACTION;
    PRINT 'Fees deleted successfully.';
    
    DBCC CHECKIDENT ('[Fees]', RESEED, 0);
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Error: ' + ERROR_MESSAGE();
END CATCH
GO
*/

-- ============================================================
-- OPTIONAL: DELETE ONLY FINES (Keep Fees)
-- ============================================================
-- Uncomment and run this section if you only want to delete fines
-- ============================================================
/*
BEGIN TRANSACTION;
BEGIN TRY
    -- Clear RemittanceId from Fines
    UPDATE [dbo].[Fines] SET RemittanceId = NULL WHERE RemittanceId IS NOT NULL;
    
    -- Delete RemittanceItems for Fines only
    DELETE FROM [dbo].[RemittanceItems] WHERE FineId IS NOT NULL;
    
    -- Delete Fine-type Remittances only
    DELETE FROM [dbo].[Remittances] WHERE RemittanceType = 'Fine';
    
    -- Delete all Fines
    DELETE FROM [dbo].[Fines];
    
    COMMIT TRANSACTION;
    PRINT 'Fines deleted successfully.';
    
    DBCC CHECKIDENT ('[Fines]', RESEED, 0);
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT 'Error: ' + ERROR_MESSAGE();
END CATCH
GO
*/

-- ============================================================
-- BACKUP COMMAND (Run this FIRST before any deletion!)
-- ============================================================
/*
BACKUP DATABASE [PortaliBITS] 
TO DISK = 'C:\Backup\PortaliBITS_Backup_BeforePaymentDelete.bak'
WITH FORMAT, COMPRESSION, 
NAME = 'Full Backup Before Payment Deletion';
GO
*/
-- ============================================================
