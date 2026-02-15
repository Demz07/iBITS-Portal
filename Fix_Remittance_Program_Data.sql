-- ============================================================
-- Fix Remittance Program Data
-- Updates existing remittances with correct Program from Student.Course
-- ============================================================

USE [PortaliBITS];
GO

PRINT '=== BEFORE UPDATE ===';
GO

-- Show current incorrect data
SELECT TOP 10
    RemittanceId,
    Program,
    Section,
    SubmittedBy,
    FeeName,
    FineCategory,
    RemittanceType,
    Status
FROM Remittances
ORDER BY RemittanceId DESC;
GO

PRINT '';
PRINT '=== UPDATING REMITTANCES ===';
GO

-- Update remittances with correct Program from Student.Course
-- Join directly from Remittances.SubmittedBy to Student.StudentNum
UPDATE r
SET r.Program = s.Course
FROM Remittances r
INNER JOIN Student s ON r.SubmittedBy = s.StudentNum
WHERE r.Program IS NULL 
   OR r.Program = r.Section 
   OR r.Program NOT IN ('BSIT', 'DIT', 'BSIS', 'BSCS');
GO

PRINT 'Remittances updated successfully!';
PRINT '';
PRINT '=== AFTER UPDATE ===';
GO

-- Verify the fix
SELECT TOP 10
    RemittanceId,
    Program,
    Section,
    SubmittedBy,
    FeeName,
    FineCategory,
    RemittanceType,
    Status
FROM Remittances
ORDER BY RemittanceId DESC;
GO

PRINT '';
PRINT '=== SUMMARY ===';
GO

SELECT 
    Program,
    COUNT(*) as RemittanceCount
FROM Remittances
GROUP BY Program
ORDER BY Program;
GO

PRINT '';
PRINT 'Update completed successfully!';
GO