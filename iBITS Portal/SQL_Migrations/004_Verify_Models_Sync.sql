-- ========================================================================================
-- iBITS Portal - Update Models for Semester System
-- Purpose: Run after updating Entity Framework models to sync database
-- Created: 2025-02-05
-- ========================================================================================

USE [PortaliBITS]
GO

PRINT N'========================================================================';
PRINT N'iBITS Portal - Models Update';
PRINT N'Started at: ' + CONVERT(VARCHAR(50), GETDATE(), 120);
PRINT N'========================================================================';
PRINT N'';

-- This script is just for verification - your models should be up to date
-- Run: dotnet ef migrations add UpdateModelsForSemesterSystem
-- Then: dotnet ef database update

PRINT N'Checking if models are in sync...';

-- Check if Course column exists in StudentSemesters table
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'StudentSemesters' AND COLUMN_NAME = 'Course')
BEGIN
    PRINT N'✓ Course column exists in StudentSemesters';
END
ELSE
BEGIN
    PRINT N'⚠ Course column missing in StudentSemesters';
END

-- Check if attendance tracking columns exist in StudentSemesters table
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'StudentSemesters' AND COLUMN_NAME IN ('TotalAbsences', 'TotalTardies', 'TotalEarlyExits', 'TotalLeaves', 'AttendancePercentage'))
BEGIN
    PRINT N'✓ Attendance tracking columns exist in StudentSemesters';
END
ELSE
BEGIN
    PRINT N'⚠ Attendance tracking columns missing in StudentSemesters';
END

-- Check if TimeIn/TimeOut columns exist in Attendance table
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
           WHERE TABLE_NAME = 'Attendance' AND COLUMN_NAME IN ('TimeIn', 'TimeOut', 'DurationMinutes', 'ScanDevice', 'Location'))
BEGIN
    PRINT N'✓ Time tracking columns exist in Attendance';
END
ELSE
BEGIN
    PRINT N'⚠ Time tracking columns missing in Attendance';
END

PRINT N'';
PRINT N'Recommended actions:';
PRINT N'1. Run: dotnet ef migrations add UpdateModelsForSemesterSystem';
PRINT N'2. Run: dotnet ef database update';
PRINT N'';

PRINT N'========================================================================';
PRINT N'MODELS UPDATE VERIFICATION COMPLETED';
PRINT N'========================================================================';
GO