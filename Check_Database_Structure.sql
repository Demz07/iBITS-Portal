-- ============================================================
-- CHECK DATABASE STRUCTURE
-- This script will show all information about the Remittances table
-- ============================================================

USE [PortaliBITS];
GO

-- Check if Remittances table exists
IF OBJECT_ID(N'[dbo].[Remittances]', N'U') IS NOT NULL
BEGIN
    PRINT 'Remittances table EXISTS';
    
    -- Show all columns in Remittances table
    SELECT 
        c.COLUMN_NAME,
        c.DATA_TYPE,
        c.CHARACTER_MAXIMUM_LENGTH,
        c.IS_NULLABLE,
        c.COLUMN_DEFAULT
    FROM INFORMATION_SCHEMA.COLUMNS c
    WHERE c.TABLE_NAME = 'Remittances'
    ORDER BY c.ORDINAL_POSITION;
    
    -- Show Primary Key
    SELECT 
        'PRIMARY KEY' AS ConstraintType,
        kcu.COLUMN_NAME
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu 
        ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
    WHERE tc.TABLE_NAME = 'Remittances' 
        AND tc.CONSTRAINT_TYPE = 'PRIMARY KEY';
    
    -- Show Foreign Keys
    SELECT 
        'FOREIGN KEY' AS ConstraintType,
        fk.name AS ForeignKeyName,
        OBJECT_NAME(fk.parent_object_id) AS TableName,
        COL_NAME(fc.parent_object_id, fc.parent_column_id) AS ColumnName,
        OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable,
        COL_NAME(fc.referenced_object_id, fc.referenced_column_id) AS ReferencedColumn
    FROM sys.foreign_keys AS fk
    INNER JOIN sys.foreign_key_columns AS fc 
        ON fk.object_id = fc.constraint_object_id
    WHERE OBJECT_NAME(fk.parent_object_id) = 'Remittances';
    
    -- Count records
    SELECT COUNT(*) AS TotalRemittances FROM [dbo].[Remittances];
    
    -- Show sample data
    SELECT TOP 5 * FROM [dbo].[Remittances];
END
ELSE
BEGIN
    PRINT 'ERROR: Remittances table does NOT exist!';
END
GO
