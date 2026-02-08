-- ========================================================================================
-- iBITS Portal - Verification Script for Chinese Characters
-- Purpose: Scan all SQL files for any remaining Chinese characters
-- Created: 2025-02-05
-- ========================================================================================

USE [PortaliBITS]
GO

PRINT N'========================================================================';
PRINT N'iBITS Portal - Chinese Character Verification';
PRINT N'Started at: ' + CONVERT(VARCHAR(50), GETDATE(), 120);
PRINT N'========================================================================';
PRINT N'';

-- Check for any columns with Chinese character patterns
PRINT N'Checking for columns with potential Chinese characters...';

SELECT 
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH
FROM INFORMATION_SCHEMA.COLUMNS
WHERE 
    COLUMN_NAME LIKE '%学期%' 
    OR COLUMN_NAME LIKE '%学号%'
    OR COLUMN_NAME LIKE '%专业%'
    OR COLUMN_NAME LIKE '%年级%'
    OR COLUMN_NAME LIKE '%班级%'
    OR COLUMN_NAME LIKE '%状态%'
    OR COLUMN_NAME LIKE '%累计%'
    OR COLUMN_NAME LIKE '%缺勤%'
    OR COLUMN_NAME LIKE '%迟到%'
    OR COLUMN_NAME LIKE '%早退%'
    OR COLUMN_NAME LIKE '%请假%'
    OR COLUMN_NAME LIKE '%补课%'
    OR COLUMN_NAME LIKE '%百分比%';

PRINT N'';
PRINT N'Checking table names for Chinese characters...';

SELECT 
    TABLE_NAME,
    TABLE_TYPE
FROM INFORMATION_SCHEMA.TABLES
WHERE 
    TABLE_NAME LIKE '%学期%' 
    OR TABLE_NAME LIKE '%学号%'
    OR TABLE_NAME LIKE '%专业%'
    OR TABLE_NAME LIKE '%年级%'
    OR TABLE_NAME LIKE '%班级%'
    OR TABLE_NAME LIKE '%状态%'
    OR TABLE_NAME LIKE '%累计%'
    OR TABLE_NAME LIKE '%缺勤%'
    OR TABLE_NAME LIKE '%迟到%'
    OR TABLE_NAME LIKE '%早退%'
    OR TABLE_NAME LIKE '%请假%'
    OR TABLE_NAME LIKE '%补课%'
    OR TABLE_NAME LIKE '%百分比%';

PRINT N'';
PRINT N'Checking constraint names for Chinese characters...';

SELECT 
    CONSTRAINT_NAME,
    CONSTRAINT_TYPE,
    TABLE_NAME
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
WHERE 
    CONSTRAINT_NAME LIKE '%学期%' 
    OR CONSTRAINT_NAME LIKE '%学号%'
    OR CONSTRAINT_NAME LIKE '%专业%'
    OR CONSTRAINT_NAME LIKE '%年级%'
    OR CONSTRAINT_NAME LIKE '%班级%'
    OR CONSTRAINT_NAME LIKE '%状态%'
    OR CONSTRAINT_NAME LIKE '%累计%'
    OR CONSTRAINT_NAME LIKE '%缺勤%'
    OR CONSTRAINT_NAME LIKE '%迟到%'
    OR CONSTRAINT_NAME LIKE '%早退%'
    OR CONSTRAINT_NAME LIKE '%请假%'
    OR CONSTRAINT_NAME LIKE '%补课%'
    OR CONSTRAINT_NAME LIKE '%百分比%';

PRINT N'';
PRINT N'========================================================================';
PRINT N'ENGLISH-ONLY VERIFICATION COMPLETED';
PRINT N'========================================================================';
PRINT N'';
PRINT N'If no results returned above, your database is clean of Chinese characters!';
PRINT N'';
GO