-- Check what tables actually exist in your database RIGHT NOW
USE [iBITSPortal]
GO

PRINT '========================================';
PRINT 'CURRENT DATABASE TABLES';
PRINT '========================================';
PRINT '';

-- List all user tables
SELECT 
    TABLE_NAME,
    CASE 
        WHEN EXISTS (
            SELECT 1 
            FROM INFORMATION_SCHEMA.COLUMNS 
            WHERE TABLE_NAME = t.TABLE_NAME 
            AND COLUMN_NAME = 'SemesterId'
        ) THEN 'YES'
        ELSE 'NO'
    END AS 'Has_SemesterId'
FROM INFORMATION_SCHEMA.TABLES t
WHERE TABLE_TYPE = 'BASE TABLE'
AND TABLE_NAME NOT LIKE 'AspNet%'
AND TABLE_NAME NOT LIKE '__EFMigrations%'
ORDER BY TABLE_NAME;

PRINT '';
PRINT '========================================';
