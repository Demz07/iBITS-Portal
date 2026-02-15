-- ============================================================
-- MIGRATION: 010_Backfill_RemittanceItems_For_Fees.sql
-- PURPOSE: Create RemittanceItems for existing fee remittances
-- ISSUE: Fee remittances created before the fix don't have RemittanceItems
-- DATE: 2026-02-11
-- ============================================================

-- This script fixes fee remittances that were created without RemittanceItems
-- It creates RemittanceItem records based on the fees linked to each remittance

USE PortaliBits;
GO

SET NOCOUNT ON;
GO

PRINT '============================================================';
PRINT 'Starting RemittanceItems Backfill Migration';
PRINT 'Date: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================================';
PRINT '';

-- ============================================================
-- STEP 1: PRE-MIGRATION ANALYSIS
-- ============================================================

PRINT '------------------------------------------------------------';
PRINT 'STEP 1: Pre-Migration Analysis';
PRINT '------------------------------------------------------------';

-- Count fee remittances without RemittanceItems
DECLARE @FeeRemittancesWithoutItems INT;
SELECT @FeeRemittancesWithoutItems = COUNT(DISTINCT r.RemittanceId)
FROM Remittances r
LEFT JOIN RemittanceItems ri ON r.RemittanceId = ri.RemittanceId
WHERE r.RemittanceType = 'Fee'
  AND ri.RemittanceItemId IS NULL;

PRINT 'Fee remittances without RemittanceItems: ' + CAST(@FeeRemittancesWithoutItems AS VARCHAR);

-- Count total fees linked to these remittances
DECLARE @FeesNeedingItems INT;
SELECT @FeesNeedingItems = COUNT(*)
FROM Fees f
INNER JOIN Remittances r ON f.RemittanceId = r.RemittanceId
LEFT JOIN RemittanceItems ri ON ri.RemittanceId = r.RemittanceId AND ri.FeeId = f.FeeId
WHERE r.RemittanceType = 'Fee'
  AND ri.RemittanceItemId IS NULL;

PRINT 'Fee payments needing RemittanceItems: ' + CAST(@FeesNeedingItems AS VARCHAR);

-- Show breakdown by remittance
PRINT '';
PRINT 'Affected Remittances:';
SELECT 
    r.RemittanceId,
    r.BatchCode,
    r.FeeName,
    r.Section,
    r.Status,
    r.TotalStudents AS [Expected Items],
    COUNT(f.FeeId) AS [Actual Fees],
    r.SubmittedDate
FROM Remittances r
LEFT JOIN RemittanceItems ri ON r.RemittanceId = ri.RemittanceId
LEFT JOIN Fees f ON f.RemittanceId = r.RemittanceId
WHERE r.RemittanceType = 'Fee'
  AND ri.RemittanceItemId IS NULL
GROUP BY r.RemittanceId, r.BatchCode, r.FeeName, r.Section, r.Status, r.TotalStudents, r.SubmittedDate
ORDER BY r.SubmittedDate DESC;

PRINT '';

-- ============================================================
-- STEP 2: VALIDATION CHECKS
-- ============================================================

PRINT '------------------------------------------------------------';
PRINT 'STEP 2: Validation Checks';
PRINT '------------------------------------------------------------';

-- Check if there are any orphaned fees (fees with RemittanceId but remittance doesn't exist)
DECLARE @OrphanedFees INT;
SELECT @OrphanedFees = COUNT(*)
FROM Fees f
WHERE f.RemittanceId IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM Remittances r WHERE r.RemittanceId = f.RemittanceId);

IF @OrphanedFees > 0
BEGIN
    PRINT 'WARNING: Found ' + CAST(@OrphanedFees AS VARCHAR) + ' orphaned fees (RemittanceId points to non-existent remittance)';
    PRINT 'These will be skipped in the migration.';
    PRINT '';
END
ELSE
BEGIN
    PRINT 'OK: No orphaned fees found.';
    PRINT '';
END

-- ============================================================
-- STEP 3: BACKUP CURRENT STATE
-- ============================================================

PRINT '------------------------------------------------------------';
PRINT 'STEP 3: Creating backup tables';
PRINT '------------------------------------------------------------';

-- Backup RemittanceItems (if any exist)
IF OBJECT_ID('RemittanceItems_Backup_20260211', 'U') IS NOT NULL
    DROP TABLE RemittanceItems_Backup_20260211;

SELECT * INTO RemittanceItems_Backup_20260211 FROM RemittanceItems;
PRINT 'Created backup: RemittanceItems_Backup_20260211 (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows)';

-- Backup Fees state
IF OBJECT_ID('Fees_Backup_20260211', 'U') IS NOT NULL
    DROP TABLE Fees_Backup_20260211;

SELECT * INTO Fees_Backup_20260211 FROM Fees WHERE RemittanceId IS NOT NULL;
PRINT 'Created backup: Fees_Backup_20260211 (' + CAST(@@ROWCOUNT AS VARCHAR) + ' rows)';

PRINT '';

-- ============================================================
-- STEP 4: CREATE MISSING REMITTANCE ITEMS
-- ============================================================

PRINT '------------------------------------------------------------';
PRINT 'STEP 4: Creating RemittanceItems for Fee Remittances';
PRINT '------------------------------------------------------------';

BEGIN TRANSACTION;

BEGIN TRY
    DECLARE @ItemsCreated INT = 0;

    -- Insert RemittanceItems for all fees linked to fee remittances that don't have items
    INSERT INTO RemittanceItems (
        RemittanceId,
        FeeId,
        FineId,
        StudentNum,
        StudentName,
        Amount,
        CollectionDate,
        PaymentMethod
    )
    SELECT 
        f.RemittanceId,
        f.FeeId,
        NULL AS FineId,  -- This is a fee, not a fine
        f.StudentNum,
        COALESCE(
            CASE 
                WHEN s.StudentMn IS NULL OR s.StudentMn = '' THEN s.StudentFn + ' ' + s.StudentLn
                ELSE s.StudentFn + ' ' + s.StudentMn + ' ' + s.StudentLn
            END,
            'Unknown Student'
        ) AS StudentName,
        ISNULL(f.Amount, 0) AS Amount,
        ISNULL(f.CollectionDate, f.DateCreated) AS CollectionDate,  -- Use CollectionDate if available, otherwise DateCreated
        'Cash' AS PaymentMethod  -- Default to Cash (can't determine from old data)
    FROM Fees f
    INNER JOIN Remittances r ON f.RemittanceId = r.RemittanceId
    INNER JOIN Student s ON f.StudentNum = s.StudentNum
    LEFT JOIN RemittanceItems ri ON ri.RemittanceId = r.RemittanceId AND ri.FeeId = f.FeeId
    WHERE r.RemittanceType = 'Fee'
      AND ri.RemittanceItemId IS NULL  -- Don't create duplicates
      AND f.RemittanceId IS NOT NULL;

    SET @ItemsCreated = @@ROWCOUNT;

    PRINT 'Created ' + CAST(@ItemsCreated AS VARCHAR) + ' RemittanceItem records';

    -- ============================================================
    -- STEP 5: VERIFICATION
    -- ============================================================

    PRINT '';
    PRINT '------------------------------------------------------------';
    PRINT 'STEP 5: Post-Migration Verification';
    PRINT '------------------------------------------------------------';

    -- Count fee remittances still without items (should be 0)
    DECLARE @RemainingWithoutItems INT;
    SELECT @RemainingWithoutItems = COUNT(DISTINCT r.RemittanceId)
    FROM Remittances r
    LEFT JOIN RemittanceItems ri ON r.RemittanceId = ri.RemittanceId
    WHERE r.RemittanceType = 'Fee'
      AND ri.RemittanceItemId IS NULL;

    PRINT 'Fee remittances still without items: ' + CAST(@RemainingWithoutItems AS VARCHAR);

    -- Verify counts match
    PRINT '';
    PRINT 'Verification - Remittance Totals vs Actual Items:';
    SELECT 
        r.RemittanceId,
        r.BatchCode,
        r.FeeName,
        r.TotalStudents AS [Expected],
        COUNT(ri.RemittanceItemId) AS [Actual Items],
        CASE 
            WHEN r.TotalStudents = COUNT(ri.RemittanceItemId) THEN 'OK'
            ELSE 'MISMATCH'
        END AS [Status]
    FROM Remittances r
    LEFT JOIN RemittanceItems ri ON r.RemittanceId = ri.RemittanceId
    WHERE r.RemittanceType = 'Fee'
    GROUP BY r.RemittanceId, r.BatchCode, r.FeeName, r.TotalStudents
    HAVING r.TotalStudents != COUNT(ri.RemittanceItemId) OR COUNT(ri.RemittanceItemId) = 0
    ORDER BY r.RemittanceId;

    -- Check if verification passed
    DECLARE @MismatchCount INT;
    SELECT @MismatchCount = COUNT(*)
    FROM Remittances r
    LEFT JOIN RemittanceItems ri ON r.RemittanceId = ri.RemittanceId
    WHERE r.RemittanceType = 'Fee'
    GROUP BY r.RemittanceId, r.TotalStudents
    HAVING r.TotalStudents != COUNT(ri.RemittanceItemId);

    IF @MismatchCount > 0
    BEGIN
        PRINT '';
        PRINT 'WARNING: Found ' + CAST(@MismatchCount AS VARCHAR) + ' remittances with count mismatches!';
        PRINT 'Review the data above. You may need to manually investigate these records.';
    END
    ELSE
    BEGIN
        PRINT '';
        PRINT 'SUCCESS: All fee remittances now have matching RemittanceItems!';
    END

    COMMIT TRANSACTION;
    PRINT '';
    PRINT '============================================================';
    PRINT 'Migration completed successfully!';
    PRINT '============================================================';
    PRINT '';
    PRINT 'Summary:';
    PRINT '  - RemittanceItems created: ' + CAST(@ItemsCreated AS VARCHAR);
    PRINT '  - Remittances still missing items: ' + CAST(@RemainingWithoutItems AS VARCHAR);
    PRINT '  - Mismatched counts: ' + CAST(@MismatchCount AS VARCHAR);
    PRINT '';
    PRINT 'Backup tables created:';
    PRINT '  - RemittanceItems_Backup_20260211';
    PRINT '  - Fees_Backup_20260211';
    PRINT '';

END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    
    PRINT '';
    PRINT '============================================================';
    PRINT 'ERROR: Migration failed!';
    PRINT '============================================================';
    PRINT 'Error Number: ' + CAST(ERROR_NUMBER() AS VARCHAR);
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR);
    PRINT '';
    PRINT 'Transaction has been rolled back.';
    PRINT 'No changes were made to the database.';
    PRINT '';
    
    -- Re-throw the error
    THROW;
END CATCH;

GO
