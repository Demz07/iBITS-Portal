-- ============================================================
-- Find the Students Table Name
-- ============================================================

USE [PortaliBITS];
GO

-- Find all tables that might be the Students table
SELECT 
    TABLE_SCHEMA,
    TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
    AND (TABLE_NAME LIKE '%Student%' OR TABLE_NAME LIKE '%User%')
ORDER BY TABLE_NAME;
GO

-- Show all tables in the database
PRINT 'All tables in PortaliBITS database:';
GO

SELECT 
    TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;
GO
