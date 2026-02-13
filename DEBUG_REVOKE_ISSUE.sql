-- ============================================================
-- DEBUG: Why Can't Revoke in Org Treasurer?
-- ============================================================
-- This script checks the actual data to see why revoke isn't working
-- ============================================================

USE PortaliBits;
GO

PRINT '============================================================';
PRINT 'CHECKING FINE RECORDS - Direct Org Treasurer Payments';
PRINT '============================================================';
PRINT '';

-- Check all PAID fines that should be revocable
SELECT 
    FineId,
    StudentNum,
    Description,
    Amount,
    FinesStatus,
    RemittanceStatus,
    RemittanceId,
    IsPaymentLocked,
    PaymentLockedDate,
    LockedBy,
    CollectedBy,
    CollectionDate,
    OfficialPaymentDate,
    -- Logic check: Should be revocable?
    CASE 
        WHEN FinesStatus = 'Paid' 
             AND IsPaymentLocked = 0 
             AND RemittanceStatus = 'Remitted' 
             AND RemittanceId IS NULL 
        THEN 'YES - Should be revocable'
        WHEN FinesStatus = 'Paid' AND RemittanceId IS NOT NULL 
        THEN 'NO - Part of batch'
        WHEN FinesStatus = 'Paid' AND IsPaymentLocked = 1 
        THEN 'NO - Already locked'
        WHEN FinesStatus = 'Paid' AND RemittanceStatus != 'Remitted' 
        THEN 'NO - Not remitted yet'
        ELSE 'NO - Not paid or other reason'
    END AS CanRevoke,
    -- Logic check: Should be lockable?
    CASE 
        WHEN FinesStatus = 'Paid' 
             AND IsPaymentLocked = 0 
             AND RemittanceStatus = 'Remitted' 
             AND RemittanceId IS NULL 
        THEN 'YES - Should be lockable'
        ELSE 'NO'
    END AS CanLock
FROM Fines
WHERE FinesStatus = 'Paid'
ORDER BY FineId DESC;

PRINT '';
PRINT '============================================================';
PRINT 'CHECKING FEES RECORDS - Direct Org Treasurer Payments';
PRINT '============================================================';
PRINT '';

-- Check all PAID fees that should be revocable
SELECT 
    FeeId,
    StudentNum,
    FeeName,
    Amount,
    FeeStatus,
    RemittanceStatus,
    RemittanceId,
    IsPaymentLocked,
    PaymentLockedDate,
    LockedBy,
    CollectedBy,
    CollectionDate,
    OfficialPaymentDate,
    -- Logic check: Should be revocable?
    CASE 
        WHEN FeeStatus = 'Paid' 
             AND IsPaymentLocked = 0 
             AND RemittanceStatus = 'Remitted' 
             AND RemittanceId IS NULL 
        THEN 'YES - Should be revocable'
        WHEN FeeStatus = 'Paid' AND RemittanceId IS NOT NULL 
        THEN 'NO - Part of batch'
        WHEN FeeStatus = 'Paid' AND IsPaymentLocked = 1 
        THEN 'NO - Already locked'
        WHEN FeeStatus = 'Paid' AND RemittanceStatus != 'Remitted' 
        THEN 'NO - Not remitted yet'
        ELSE 'NO - Not paid or other reason'
    END AS CanRevoke,
    -- Logic check: Should be lockable?
    CASE 
        WHEN FeeStatus = 'Paid' 
             AND IsPaymentLocked = 0 
             AND RemittanceStatus = 'Remitted' 
             AND RemittanceId IS NULL 
        THEN 'YES - Should be lockable'
        ELSE 'NO'
    END AS CanLock
FROM Fees
WHERE FeeStatus = 'Paid'
ORDER BY FeeId DESC;

PRINT '';
PRINT '============================================================';
PRINT 'SUMMARY';
PRINT '============================================================';

DECLARE @RevocableFines INT, @RevocableFees INT;

SELECT @RevocableFines = COUNT(*)
FROM Fines
WHERE FinesStatus = 'Paid' 
  AND IsPaymentLocked = 0 
  AND RemittanceStatus = 'Remitted' 
  AND RemittanceId IS NULL;

SELECT @RevocableFees = COUNT(*)
FROM Fees
WHERE FeeStatus = 'Paid' 
  AND IsPaymentLocked = 0 
  AND RemittanceStatus = 'Remitted' 
  AND RemittanceId IS NULL;

PRINT 'Revocable Fines (Direct Org Treasurer): ' + CAST(@RevocableFines AS VARCHAR(10));
PRINT 'Revocable Fees (Direct Org Treasurer): ' + CAST(@RevocableFees AS VARCHAR(10));

IF @RevocableFines = 0 AND @RevocableFees = 0
BEGIN
    PRINT '';
    PRINT '⚠️ NO REVOCABLE PAYMENTS FOUND!';
    PRINT '';
    PRINT 'This means one of the following:';
    PRINT '1. All payments are part of remittance batches (RemittanceId IS NOT NULL)';
    PRINT '2. All payments are already locked (IsPaymentLocked = 1)';
    PRINT '3. All payments are not yet remitted (RemittanceStatus != ''Remitted'')';
    PRINT '4. No payments marked as paid by Org Treasurer directly';
    PRINT '';
    PRINT 'TO FIX: Mark a payment as paid directly as Org Treasurer (not through Class Treasurer/batch)';
END
ELSE
BEGIN
    PRINT '';
    PRINT '✅ Found revocable payments!';
    PRINT 'Check the results above to see which payments should show Revoke/Lock buttons';
END

GO
