-- ============================================================
-- DEBUG: Why Chart Doesn't Show Direct Org Treasurer Payments
-- ============================================================
-- Run this to check if direct payments are saved correctly
-- ============================================================

USE PortaliBits;
GO

PRINT '============================================================';
PRINT 'CHECKING FEES - Direct Org Treasurer Payments';
PRINT '============================================================';
PRINT '';

-- Check all PAID fees
SELECT 
    FeeId,
    StudentNum,
    FeeName,
    Amount,
    FeeStatus,
    RemittanceStatus,
    RemittanceId,
    CollectedBy,
    CollectionDate,
    OfficialPaymentDate,
    IsPaymentLocked,
    -- Check if should appear in chart
    CASE 
        WHEN FeeStatus = 'Paid' 
             AND RemittanceStatus = 'Remitted' 
        THEN 'YES - Should be in chart'
        WHEN FeeStatus = 'Paid' AND RemittanceStatus != 'Remitted' 
        THEN 'NO - Not remitted yet'
        WHEN FeeStatus = 'paid' -- lowercase
             AND RemittanceStatus = 'Remitted' 
        THEN 'YES - Should be in chart (lowercase status)'
        ELSE 'NO - Not paid or other issue'
    END AS ShouldBeInChart,
    -- Check payment source
    CASE 
        WHEN RemittanceId IS NULL AND RemittanceStatus = 'Remitted' 
        THEN 'Direct Org Treasurer Payment'
        WHEN RemittanceId IS NOT NULL 
        THEN 'Batch Remittance Payment'
        ELSE 'Class Treasurer (Not yet validated)'
    END AS PaymentSource
FROM Fees
WHERE FeeStatus IN ('Paid', 'paid', 'PAID') -- Check all casings
ORDER BY FeeId DESC;

PRINT '';
PRINT '============================================================';
PRINT 'SUMMARY - Chart Inclusion Check';
PRINT '============================================================';

DECLARE @DirectOrgPaid INT, @BatchPaid INT, @ClassPaidNotValidated INT;

-- Count direct Org Treasurer payments (should be in chart)
SELECT @DirectOrgPaid = COUNT(*)
FROM Fees
WHERE FeeStatus IN ('Paid', 'paid', 'PAID')
  AND RemittanceStatus = 'Remitted'
  AND RemittanceId IS NULL;

-- Count batch remittance payments (should be in chart)
SELECT @BatchPaid = COUNT(*)
FROM Fees
WHERE FeeStatus IN ('Paid', 'paid', 'PAID')
  AND RemittanceStatus = 'Remitted'
  AND RemittanceId IS NOT NULL;

-- Count Class Treasurer payments not yet validated (should NOT be in chart)
SELECT @ClassPaidNotValidated = COUNT(*)
FROM Fees
WHERE FeeStatus IN ('Paid', 'paid', 'PAID')
  AND (RemittanceStatus != 'Remitted' OR RemittanceStatus IS NULL);

PRINT 'Direct Org Treasurer Payments (SHOULD be in chart): ' + CAST(@DirectOrgPaid AS VARCHAR(10));
PRINT 'Batch Remittance Payments (SHOULD be in chart): ' + CAST(@BatchPaid AS VARCHAR(10));
PRINT 'Class Treasurer Payments - Not Validated (should NOT be in chart): ' + CAST(@ClassPaidNotValidated AS VARCHAR(10));

PRINT '';
PRINT '============================================================';
PRINT 'POTENTIAL ISSUES TO CHECK:';
PRINT '============================================================';

-- Check for casing issues
IF EXISTS (SELECT 1 FROM Fees WHERE FeeStatus = 'paid' COLLATE Latin1_General_CS_AS)
BEGIN
    PRINT '⚠️ WARNING: Found FeeStatus with lowercase "paid" - might cause issues!';
    SELECT COUNT(*) AS LowercaseCount FROM Fees WHERE FeeStatus = 'paid' COLLATE Latin1_General_CS_AS;
END

-- Check for NULL RemittanceStatus
IF EXISTS (SELECT 1 FROM Fees WHERE FeeStatus = 'Paid' AND RemittanceStatus IS NULL)
BEGIN
    PRINT '⚠️ WARNING: Found Paid fees with NULL RemittanceStatus!';
    SELECT COUNT(*) AS NullRemittanceStatusCount 
    FROM Fees 
    WHERE FeeStatus = 'Paid' AND RemittanceStatus IS NULL;
END

-- Check for unexpected RemittanceStatus values
SELECT DISTINCT RemittanceStatus, COUNT(*) AS Count
FROM Fees
WHERE FeeStatus IN ('Paid', 'paid', 'PAID')
GROUP BY RemittanceStatus;

PRINT '';
PRINT '============================================================';
PRINT 'CHART DATA SIMULATION (What controller should see):';
PRINT '============================================================';

-- Simulate what the chart query sees
SELECT 
    StudentNavigation.YearLevelSection,
    COUNT(*) AS FeesInThisSection,
    SUM(f.Amount) AS TotalAmount
FROM Fees f
INNER JOIN Student StudentNavigation ON f.StudentNum = StudentNavigation.StudentNum
WHERE f.FeeStatus = 'Paid' COLLATE Latin1_General_CI_AS -- Case insensitive
  AND f.RemittanceStatus = 'Remitted'
  AND StudentNavigation.YearLevelSection IS NOT NULL
GROUP BY StudentNavigation.YearLevelSection
ORDER BY StudentNavigation.YearLevelSection;

GO
