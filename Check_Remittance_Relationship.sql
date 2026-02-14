-- Check the relationship between Remittances, AspNetUsers, and Student
USE [PortaliBITS];
GO

-- Show Remittances columns
PRINT '=== Remittances Table Columns ===';
SELECT COLUMN_NAME, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Remittances'
ORDER BY ORDINAL_POSITION;
GO

-- Show AspNetUsers columns related to Student
PRINT '';
PRINT '=== AspNetUsers Columns ===';
SELECT COLUMN_NAME, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'AspNetUsers'
AND (COLUMN_NAME LIKE '%Student%' OR COLUMN_NAME = 'Id' OR COLUMN_NAME LIKE '%User%')
ORDER BY ORDINAL_POSITION;
GO

-- Show Student columns
PRINT '';
PRINT '=== Student Table Columns ===';
SELECT COLUMN_NAME, DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Student'
ORDER BY ORDINAL_POSITION;
GO

-- Check a sample remittance to see actual data
PRINT '';
PRINT '=== Sample Remittance Data ===';
SELECT TOP 1 * FROM Remittances;
GO

-- Check how SubmittedBy relates to users and students
PRINT '';
PRINT '=== Sample Join Test ===';
SELECT TOP 3
    r.RemittanceId,
    r.SubmittedBy,
    r.Program,
    r.Section
FROM Remittances r;
GO
