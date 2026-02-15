-- ============================================================
-- POPULATE Program Column from Student.Course
-- Run this AFTER the column has been added
-- ============================================================

USE [PortaliBITS];
GO

PRINT 'Populating Program column from Student Course data...';
GO

-- Update Program from Student.Course (table name is singular)
UPDATE r
SET r.[Program] = s.Course
FROM [dbo].[Remittances] r
INNER JOIN [dbo].[AspNetUsers] u ON r.SubmittedBy = u.UserName
INNER JOIN [dbo].[Student] s ON u.UserName = s.StudentNum
WHERE r.[Program] IS NULL AND s.Course IS NOT NULL;
GO

PRINT 'Program populated successfully!';
GO

-- Verify the results
SELECT 
    COUNT(*) AS TotalRemittances,
    COUNT([Program]) AS WithProgram,
    COUNT(*) - COUNT([Program]) AS WithoutProgram
FROM [dbo].[Remittances];
GO

-- Show the data
SELECT 
    RemittanceId,
    BatchCode,
    [Program],
    Section,
    RemittanceType,
    Status,
    SubmittedBy
FROM [dbo].[Remittances];
GO

PRINT 'Done!';
GO
