-- ============================================================
-- FINAL MIGRATION: Add Program Column to Remittances Table
-- Based on actual database structure analysis
-- ============================================================

USE [PortaliBITS];
GO

PRINT '=== Starting Migration ===';
GO

-- Step 1: Add the Program column
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Remittances]') 
    AND name = 'Program'
)
BEGIN
    PRINT 'Step 1: Adding Program column...';
    
    ALTER TABLE [dbo].[Remittances]
    ADD [Program] NVARCHAR(50) NULL;
    
    PRINT 'Program column added successfully!';
END
ELSE
BEGIN
    PRINT 'Program column already exists. Skipping...';
END
GO

-- Step 2: Populate Program from Section
-- Note: Your Section contains only "3-1", not "BSIT 3-1"
-- We need to get Program from the Student's Course field
PRINT 'Step 2: Populating Program column...';
GO

UPDATE r
SET r.[Program] = s.Course
FROM [dbo].[Remittances] r
INNER JOIN [dbo].[AspNetUsers] u ON r.SubmittedBy = u.UserName
INNER JOIN [dbo].[Students] s ON u.UserName = s.StudentNum
WHERE r.[Program] IS NULL AND s.Course IS NOT NULL;
GO

PRINT 'Program populated from Student Course data.';
GO

-- Step 3: Verify the migration
PRINT 'Step 3: Verification...';
GO

SELECT 
    COUNT(*) AS TotalRemittances,
    COUNT([Program]) AS WithProgram,
    COUNT(*) - COUNT([Program]) AS WithoutProgram
FROM [dbo].[Remittances];
GO

-- Show sample data
SELECT TOP 5
    RemittanceId,
    BatchCode,
    [Program],
    Section,
    RemittanceType,
    Status
FROM [dbo].[Remittances];
GO

PRINT '=== Migration Completed Successfully! ===';
GO
