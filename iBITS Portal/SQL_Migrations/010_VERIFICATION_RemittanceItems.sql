-- ============================================================
-- VERIFICATION SCRIPT: 010_VERIFICATION_RemittanceItems.sql
-- PURPOSE: Test and verify RemittanceItems migration
-- DATE: 2026-02-11
-- ============================================================

-- This script helps you verify the migration was successful
-- Run this AFTER the migration to check everything is correct

USE PortaliBits;
GO

SET NOCOUNT ON;
GO

PRINT '============================================================';
PRINT 'RemittanceItems Migration Verification';
PRINT 'Date: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================================';
PRINT '';

-- ============================================================
-- CHECK 1: Fee Remittances Without Items
-- ============================================================

PRINT '------------------------------------------------------------';
PRINT 'CHECK 1: Fee Remittances Missing RemittanceItems';
PRINT '------------------------------------------------------------';

SELECT 
    r.RemittanceId,
    r.BatchCode,
    r.FeeName,
    r.Section,
    r.Status,
    r.TotalStudents,
    r.TotalAmount,
    r.SubmittedDate,
    COUNT(ri.RemittanceItemId) AS [ActualItems]
FROM Remittances r
LEFT JOIN RemittanceItems ri ON r.RemittanceId = ri.RemittanceId
WHERE r.RemittanceType = 'Fee'
GROUP BY r.RemittanceId, r.BatchCode, r.FeeName, r.Section, r.Status, r.TotalStudents, r.TotalAmount, r.SubmittedDate
HAVING COUNT(ri.RemittanceItemId) = 0
ORDER BY r.SubmittedDate DESC;

DECLARE @MissingItems INT = @@ROWCOUNT;
IF @MissingItems = 0
    PRINT 'PASS: All fee remittances have RemittanceItems ✓';
ELSE
    PRINT 'FAIL: ' + CAST(@MissingItems AS VARCHAR) + ' fee remittances are missing RemittanceItems ✗';

PRINT '';

-- ============================================================
-- CHECK 2: Count Mismatches
-- ============================================================

PRINT '------------------------------------------------------------';
PRINT 'CHECK 2: Remittances with Item Count Mismatches';
PRINT '------------------------------------------------------------';

SELECT 
    r.RemittanceId,
    r.BatchCode,
    r.RemittanceType,
    r.FeeName,
    r.TotalStudents AS [Expected],
    COUNT(ri.RemittanceItemId) AS [Actual],
    r.TotalStudents - COUNT(ri.RemittanceItemId) AS [Difference],
    r.Status
FROM Remittances r
LEFT JOIN RemittanceItems ri ON r.RemittanceId = ri.RemittanceId
GROUP BY r.RemittanceId, r.BatchCode, r.RemittanceType, r.FeeName, r.TotalStudents, r.Status
HAVING r.TotalStudents != COUNT(ri.RemittanceItemId)
ORDER BY r.RemittanceId;

DECLARE @Mismatches INT = @@ROWCOUNT;
IF @Mismatches = 0
    PRINT 'PASS: All remittances have matching item counts ✓';
ELSE
    PRINT 'WARNING: ' + CAST(@Mismatches AS VARCHAR) + ' remittances have count mismatches ⚠';

PRINT '';

-- ============================================================
-- CHECK 3: Amount Totals Match
-- ============================================================

PRINT '------------------------------------------------------------';
PRINT 'CHECK 3: Remittance Amount Totals vs Item Sums';
PRINT '------------------------------------------------------------';

SELECT 
    r.RemittanceId,
    r.BatchCode,
    r.RemittanceType,
    r.TotalAmount AS [Expected Total],
    ISNULL(SUM(ri.Amount), 0) AS [Actual Total],
    r.TotalAmount - ISNULL(SUM(ri.Amount), 0) AS [Difference],
    r.Status
FROM Remittances r
LEFT JOIN RemittanceItems ri ON r.RemittanceId = ri.RemittanceId
GROUP BY r.RemittanceId, r.BatchCode, r.RemittanceType, r.TotalAmount, r.Status
HAVING ABS(r.TotalAmount - ISNULL(SUM(ri.Amount), 0)) > 0.01  -- Allow for rounding
ORDER BY ABS(r.TotalAmount - ISNULL(SUM(ri.Amount), 0)) DESC;

DECLARE @AmountMismatches INT = @@ROWCOUNT;
IF @AmountMismatches = 0
    PRINT 'PASS: All remittance amounts match their items ✓';
ELSE
    PRINT 'WARNING: ' + CAST(@AmountMismatches AS VARCHAR) + ' remittances have amount mismatches ⚠';

PRINT '';

-- ============================================================
-- CHECK 4: Fees with RemittanceId but No RemittanceItem
-- ============================================================

PRINT '------------------------------------------------------------';
PRINT 'CHECK 4: Fees Linked to Remittance but Missing RemittanceItem';
PRINT '------------------------------------------------------------';

SELECT 
    f.FeeId,
    f.StudentNum,
    s.FullName,
    f.FeeName,
    f.Amount,
    f.RemittanceId,
    r.BatchCode,
    f.RemittanceStatus
FROM Fees f
INNER JOIN Students s ON f.StudentNum = s.StudentNum
INNER JOIN Remittances r ON f.RemittanceId = r.RemittanceId
WHERE f.RemittanceId IS NOT NULL
  AND NOT EXISTS (
      SELECT 1 FROM RemittanceItems ri 
      WHERE ri.RemittanceId = f.RemittanceId 
        AND ri.FeeId = f.FeeId
  )
ORDER BY f.RemittanceId, f.StudentNum;

DECLARE @OrphanedFees INT = @@ROWCOUNT;
IF @OrphanedFees = 0
    PRINT 'PASS: All fees with RemittanceId have corresponding RemittanceItems ✓';
ELSE
    PRINT 'FAIL: ' + CAST(@OrphanedFees AS VARCHAR) + ' fees are missing RemittanceItems ✗';

PRINT '';

-- ============================================================
-- CHECK 5: Overall Statistics
-- ============================================================

PRINT '------------------------------------------------------------';
PRINT 'CHECK 5: Overall Database Statistics';
PRINT '------------------------------------------------------------';

PRINT 'Remittances by Type and Status:';
SELECT 
    RemittanceType,
    Status,
    COUNT(*) AS [Count],
    SUM(TotalStudents) AS [Total Students],
    SUM(TotalAmount) AS [Total Amount]
FROM Remittances
GROUP BY RemittanceType, Status
ORDER BY RemittanceType, Status;

PRINT '';
PRINT 'RemittanceItems by Remittance Type:';
SELECT 
    r.RemittanceType,
    COUNT(ri.RemittanceItemId) AS [Item Count],
    SUM(ri.Amount) AS [Total Amount]
FROM RemittanceItems ri
INNER JOIN Remittances r ON ri.RemittanceId = r.RemittanceId
GROUP BY r.RemittanceType
ORDER BY r.RemittanceType;

PRINT '';
PRINT 'Fees by Remittance Status:';
SELECT 
    RemittanceStatus,
    COUNT(*) AS [Count],
    SUM(ISNULL(Amount, 0)) AS [Total Amount]
FROM Fees
GROUP BY RemittanceStatus
ORDER BY RemittanceStatus;

PRINT '';

-- ============================================================
-- CHECK 6: Sample Data Verification
-- ============================================================

PRINT '------------------------------------------------------------';
PRINT 'CHECK 6: Sample Remittance with Details (Most Recent Fee)';
PRINT '------------------------------------------------------------';

-- Get the most recent fee remittance
DECLARE @SampleRemittanceId INT;
SELECT TOP 1 @SampleRemittanceId = RemittanceId
FROM Remittances
WHERE RemittanceType = 'Fee'
ORDER BY SubmittedDate DESC;

IF @SampleRemittanceId IS NOT NULL
BEGIN
    PRINT 'Sample Remittance Details:';
    SELECT 
        RemittanceId,
        BatchCode,
        FeeName,
        Section,
        TotalStudents,
        TotalAmount,
        Status,
        SubmittedDate
    FROM Remittances
    WHERE RemittanceId = @SampleRemittanceId;

    PRINT '';
    PRINT 'Sample Remittance Items:';
    SELECT 
        RemittanceItemId,
        StudentNum,
        StudentName,
        Amount,
        CollectionDate,
        PaymentMethod
    FROM RemittanceItems
    WHERE RemittanceId = @SampleRemittanceId
    ORDER BY StudentName;

    DECLARE @SampleItemCount INT;
    SELECT @SampleItemCount = COUNT(*) FROM RemittanceItems WHERE RemittanceId = @SampleRemittanceId;
    
    PRINT '';
    PRINT 'Items in sample remittance: ' + CAST(@SampleItemCount AS VARCHAR);
END
ELSE
BEGIN
    PRINT 'No fee remittances found in database.';
END

PRINT '';

-- ============================================================
-- FINAL SUMMARY
-- ============================================================

PRINT '============================================================';
PRINT 'VERIFICATION SUMMARY';
PRINT '============================================================';
PRINT '';
PRINT 'Results:';
PRINT '  - Fee remittances missing items: ' + CAST(@MissingItems AS VARCHAR);
PRINT '  - Count mismatches: ' + CAST(@Mismatches AS VARCHAR);
PRINT '  - Amount mismatches: ' + CAST(@AmountMismatches AS VARCHAR);
PRINT '  - Orphaned fees: ' + CAST(@OrphanedFees AS VARCHAR);
PRINT '';

DECLARE @TotalIssues INT = @MissingItems + @Mismatches + @AmountMismatches + @OrphanedFees;

IF @TotalIssues = 0
BEGIN
    PRINT '✓✓✓ ALL CHECKS PASSED ✓✓✓';
    PRINT 'The migration was successful!';
END
ELSE
BEGIN
    PRINT '⚠⚠⚠ ISSUES DETECTED ⚠⚠⚠';
    PRINT 'Total issues found: ' + CAST(@TotalIssues AS VARCHAR);
    PRINT 'Please review the details above.';
END

PRINT '';
PRINT '============================================================';

GO
