-- ========================================================================================
-- iBITS Portal - Database Structure Verification
-- Purpose: Check current database structure to understand exactly what we have
-- Created: 2025-02-05
-- ========================================================================================

USE [PortaliBITS]
GO

PRINT N'========================================================================';
PRINT N'DATABASE STRUCTURE VERIFICATION';
PRINT N'Started at: ' + CONVERT(VARCHAR(50), GETDATE(), 120);
PRINT N'========================================================================';
PRINT N'';

PRINT N'=== Student Table Columns ===';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'Student' 
ORDER BY ORDINAL_POSITION;

PRINT N'';
PRINT N'=== StudentSemesters Table Columns ===';
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE, COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME = 'StudentSemesters' 
ORDER BY ORDINAL_POSITION;

PRINT N'';
PRINT N'=== Current Semester Information ===';
SELECT SemesterId, SemesterName, AcademicYearId, IsCurrent, StartDate, EndDate
FROM Semesters 
WHERE IsCurrent = 1;

PRINT N'';
PRINT N'=== Academic Years Information ===';
SELECT AcademicYearId, YearName, StartDate, EndDate, IsActive
FROM AcademicYears 
ORDER BY YearName;

PRINT N'';
PRINT N'=== Student Enrollment Count ===';
SELECT 
    COUNT(*) as TotalStudents,
    COUNT(CASE WHEN IsArchived = 0 THEN 1 END) as ActiveStudents,
    COUNT(CASE WHEN SemesterId IS NOT NULL THEN 1 END) as StudentsWithSemester
FROM Student;

PRINT N'';
PRINT N'=== StudentSemesters Enrollment Count ===';
SELECT 
    COUNT(*) as TotalEnrollments,
    COUNT(CASE WHEN EnrollmentStatus = 'Active' THEN 1 END) as ActiveEnrollments,
    COUNT(DISTINCT StudentNum) as UniqueStudents
FROM StudentSemesters;

PRINT N'';
PRINT N'========================================================================';
PRINT N'VERIFICATION COMPLETED';
PRINT N'========================================================================';
GO