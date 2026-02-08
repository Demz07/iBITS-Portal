-- =============================================
-- CREATE NEW FRESH SEMESTER
-- This script helps you start a new semester with no data
-- Old data stays in the old semester (archived)
-- =============================================

USE [PortaliBITS]
GO

-- =============================================
-- INSTRUCTIONS:
-- 1. Update the variables below with your new semester details
-- 2. Run this script
-- 3. All old records will stay in the old semester
-- 4. New semester will be empty and ready for new records
-- =============================================

DECLARE @NewSemesterName NVARCHAR(50) = '2nd Semester';  -- CHANGE THIS
DECLARE @NewAcademicYearName NVARCHAR(20) = 'A.Y. 2025-2026';  -- CHANGE THIS
DECLARE @StartDate DATE = '2026-02-08';  -- CHANGE THIS
DECLARE @EndDate DATE = '2026-06-30';  -- CHANGE THIS

-- =============================================
-- DO NOT MODIFY BELOW THIS LINE
-- =============================================

BEGIN TRANSACTION;

PRINT '========================================';
PRINT 'CREATING NEW FRESH SEMESTER';
PRINT '========================================';
PRINT '';

-- Find or create Academic Year
DECLARE @AcademicYearId INT;
SELECT @AcademicYearId = AcademicYearId 
FROM AcademicYears 
WHERE YearName = @NewAcademicYearName;

IF @AcademicYearId IS NULL
BEGIN
    INSERT INTO AcademicYears (YearName, IsActive)
    VALUES (@NewAcademicYearName, 1);
    
    SET @AcademicYearId = SCOPE_IDENTITY();
    PRINT '✓ Created new Academic Year: ' + @NewAcademicYearName;
END
ELSE
BEGIN
    PRINT '✓ Using existing Academic Year: ' + @NewAcademicYearName;
END

PRINT '';

-- Set all existing semesters to NOT current
UPDATE Semesters 
SET IsCurrent = 0;

PRINT '✓ Marked all existing semesters as historical';
PRINT '';

-- Create new semester
INSERT INTO Semesters (
    AcademicYearId, 
    SemesterName, 
    StartDate, 
    EndDate, 
    IsCurrent, 
    IsActive
)
VALUES (
    @AcademicYearId,
    @NewSemesterName,
    @StartDate,
    @EndDate,
    1,  -- This is the current semester
    1   -- Active
);

DECLARE @NewSemesterId INT = SCOPE_IDENTITY();

PRINT '✓ Created new semester:';
PRINT '  Semester ID: ' + CAST(@NewSemesterId AS VARCHAR(10));
PRINT '  Name: ' + @NewAcademicYearName + ' - ' + @NewSemesterName;
PRINT '  Start Date: ' + CONVERT(VARCHAR(10), @StartDate, 120);
PRINT '  End Date: ' + CONVERT(VARCHAR(10), @EndDate, 120);
PRINT '  Status: CURRENT & ACTIVE';
PRINT '';

-- Show statistics
PRINT '========================================';
PRINT 'NEW SEMESTER CREATED SUCCESSFULLY!';
PRINT '========================================';
PRINT '';
PRINT 'Summary:';
PRINT '  - Old semesters: Archived (data preserved)';
PRINT '  - New semester: Empty and ready for new records';
PRINT '  - All new Fees, Fines, Events will go to Semester ' + CAST(@NewSemesterId AS VARCHAR(10));
PRINT '';
PRINT 'What happens now:';
PRINT '  ✓ Old data stays in old semesters (archived)';
PRINT '  ✓ New Fees/Fines/Events automatically use new semester';
PRINT '  ✓ Semester dropdown shows new semester as current (⭐)';
PRINT '';
PRINT 'Note: If historical viewing is disabled, users can only';
PRINT '      see the current semester data.';
PRINT '========================================';

COMMIT TRANSACTION;
GO
