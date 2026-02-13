-- ============================================================
-- SIMPLE MIGRATION: Add Program Column to Remittances Table
-- This is a simplified version that should work
-- ============================================================

USE [PortaliBITS];
GO

-- Step 1: Add the column (simple approach)
IF NOT EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Remittances]') 
    AND name = 'Program'
)
BEGIN
    PRINT 'Adding Program column...';
    
    ALTER TABLE [dbo].[Remittances]
    ADD [Program] NVARCHAR(50) NULL;
    
    PRINT 'Program column added successfully.';
END
ELSE
BEGIN
    PRINT 'Program column already exists.';
END
GO

-- Step 2: Update existing records (separate batch)
IF EXISTS (
    SELECT * FROM sys.columns 
    WHERE object_id = OBJECT_ID(N'[dbo].[Remittances]') 
    AND name = 'Program'
)
BEGIN
    PRINT 'Populating Program column from Section...';
    
    UPDATE [dbo].[Remittances]
    SET [Program] = 
        CASE 
            WHEN CHARINDEX(' ', [Section]) > 0 
            THEN LEFT([Section], CHARINDEX(' ', [Section]) - 1)
            ELSE [Section]
        END
    WHERE [Section] IS NOT NULL;
    
    PRINT 'Program column populated successfully.';
END
GO

-- Step 3: Verify
SELECT 
    COUNT(*) AS TotalRemittances,
    COUNT([Program]) AS WithProgram,
    COUNT(*) - COUNT([Program]) AS WithoutProgram
FROM [dbo].[Remittances];
GO

PRINT 'Migration completed!';
GO
