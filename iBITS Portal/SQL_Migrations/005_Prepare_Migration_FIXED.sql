-- ========================================================================================
-- iBITS Portal - EF Core Migration for Semester System
-- Purpose: Generate EF Core migration for semester and time tracking
-- Created: 2025-02-05
-- Fixed: T-SQL syntax errors and quote issues
-- ========================================================================================

PRINT N'========================================================================';
PRINT N'iBITS Portal - EF Core Migration Preparation';
PRINT N'Started at: ' + CONVERT(VARCHAR(50), GETDATE(), 120);
PRINT N'========================================================================';
PRINT N'';

PRINT N'VERIFICATION CHECKS:';
PRINT N'';

-- Check if Semester tables exist
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME IN ('AcademicYears', 'Semesters', 'StudentSemesters'))
BEGIN
    PRINT N'✓ Semester system tables exist';
END
ELSE
BEGIN
    PRINT N'⚠ Semester tables missing - run database migration';
END

-- Check if time tracking columns exist in Attendance
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'Attendance' AND COLUMN_NAME IN ('TimeIn', 'TimeOut', 'DurationMinutes'))
BEGIN
    PRINT N'✓ Time tracking columns exist in Attendance';
END
ELSE
BEGIN
    PRINT N'⚠ Time tracking columns missing - run database migration';
END

-- Check if QRAuditLog table exists
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'QRAuditLog')
BEGIN
    PRINT N'✓ QRAuditLog table exists';
END
ELSE
BEGIN
    PRINT N'⚠ QRAuditLog table missing - run database migration';
END

PRINT N'';
PRINT N'MIGRATION STATUS: ' + CASE 
    WHEN EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME IN ('AcademicYears', 'Semesters', 'StudentSemesters'))
    AND EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Attendance' AND COLUMN_NAME IN ('TimeIn', 'TimeOut', 'DurationMinutes'))
    AND EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'QRAuditLog')
    THEN 'READY FOR MIGRATION'
    ELSE 'NEEDS DATABASE MIGRATION'
END;

PRINT N'';
PRINT N'========================================================================';
PRINT N'PREPARATION VERIFICATION COMPLETED';
PRINT N'========================================================================';
GO