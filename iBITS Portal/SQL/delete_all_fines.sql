-- ============================================================
-- Script to DELETE all fines and related records
-- WARNING: This will delete ALL fine data permanently!
-- ============================================================

-- Step 1: Delete RemittanceItems that reference Fines
DELETE FROM [RemittanceItems] WHERE FineId IS NOT NULL;

-- Step 2: Delete FinePaymentTransactions (if this table exists)
-- Uncomment if you have this table:
-- DELETE FROM [FinePaymentTransactions];

-- Step 3: Delete Remittances that are for Fines (optional - if you want to keep remittance history, skip this)
-- This deletes only Fine-type remittances, not Fee remittances
-- RemittanceType enum: 0 = Fee, 1 = Fine
DELETE FROM [Remittances] WHERE RemittanceType = 1;

-- Step 4: Delete all Fines
DELETE FROM [Fines];

-- Step 5: RESET the ID counter to 0 (so the next one starts at 1)
DBCC CHECKIDENT ('[Fines]', RESEED, 0);

-- Step 6: Verify deletion
SELECT COUNT(*) AS RemainingFines FROM [Fines];
SELECT COUNT(*) AS RemainingFineRemittanceItems FROM [RemittanceItems] WHERE FineId IS NOT NULL;
SELECT COUNT(*) AS RemainingFineRemittances FROM [Remittances] WHERE RemittanceType = 1;

PRINT 'All fines and related records have been deleted successfully!';
