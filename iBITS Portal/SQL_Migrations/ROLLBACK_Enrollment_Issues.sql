-- ========================================================================================
-- iBITS Portal - ROLLBACK Failed Enrollment Section
-- Purpose: Clean up any partial enrollment issues
-- Created: 2025-02-05
-- ========================================================================================

USE [PortaliBITS]
GO

PRINT N'========================================================================';
PRINT N'iBITS Portal - Rolling Back Enrollment Issues';
PRINT N'Started at: ' + CONVERT(VARCHAR(50), GETDATE(), 120);
PRINT N'========================================================================';
PRINT N'';

BEGIN TRANSACTION;

-- Clean up any invalid enrollment records that might have been created
PRINT N'Cleaning up invalid enrollment records...';

DELETE FROM [dbo].[StudentSemesters] 
WHERE SemesterId IS NULL
OR StudentNum IS NULL
OR YearLevel IS NULL;

PRINT N'    ✓ Cleaned up invalid records';

COMMIT TRANSACTION;

PRINT N'✓ Rollback completed successfully';
PRINT N'';
PRINT N'Ready to run the fixed script!';
PRINT N'';
GO