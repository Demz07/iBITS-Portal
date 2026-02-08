-- =============================================
-- CLEANUP: Keep Only A.Y. 2025-2026 - 1st Semester
-- Deletes all other semesters and moves all records to this one
-- =============================================

USE [PortaliBITS]
GO

BEGIN TRANSACTION;

PRINT '========================================';
PRINT 'CLEANUP: KEEP ONLY ONE SEMESTER';
PRINT '========================================';
PRINT '';

-- =============================================
-- STEP 1: Find or verify the target semester exists
-- =============================================

DECLARE @TargetSemesterId INT;
DECLARE @TargetAcademicYearName NVARCHAR(50) = 'A.Y. 2025-2026';
DECLARE @TargetSemesterName NVARCHAR(50) = '1st Semester';

-- Find the target semester
SELECT @TargetSemesterId = s.SemesterId
FROM Semesters s
INNER JOIN AcademicYears ay ON s.AcademicYearId = ay.AcademicYearId
WHERE ay.YearName = @TargetAcademicYearName
  AND s.SemesterName = @TargetSemesterName;

-- If doesn't exist, create it
IF @TargetSemesterId IS NULL
BEGIN
    PRINT 'Target semester not found. Creating it...';
    
    -- Find or create Academic Year
    DECLARE @AcademicYearId INT;
    SELECT @AcademicYearId = AcademicYearId 
    FROM AcademicYears 
    WHERE YearName = @TargetAcademicYearName;
    
    IF @AcademicYearId IS NULL
    BEGIN
        INSERT INTO AcademicYears (YearName, IsActive)
        VALUES (@TargetAcademicYearName, 1);
        
        SET @AcademicYearId = SCOPE_IDENTITY();
        PRINT '  Created Academic Year: ' + @TargetAcademicYearName;
    END
    
    -- Create the semester
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
        @TargetSemesterName,
        '2025-08-01',  -- Default start date
        '2025-12-31',  -- Default end date
        1,  -- Set as current
        1   -- Active
    );
    
    SET @TargetSemesterId = SCOPE_IDENTITY();
    PRINT '  Created Semester: ' + @TargetSemesterName;
    PRINT '';
END

PRINT 'Target Semester: ' + @TargetAcademicYearName + ' - ' + @TargetSemesterName;
PRINT 'Semester ID: ' + CAST(@TargetSemesterId AS VARCHAR(10));
PRINT '';

-- =============================================
-- STEP 2: Move ALL records to target semester
-- =============================================

PRINT 'Moving all records to target semester...';
PRINT '';

DECLARE @Count INT;

-- Update Fees
UPDATE Fees SET SemesterId = @TargetSemesterId;
SET @Count = @@ROWCOUNT;
PRINT '  Fees: ' + CAST(@Count AS VARCHAR(10)) + ' records';

-- Update Fines
UPDATE Fines SET SemesterId = @TargetSemesterId;
SET @Count = @@ROWCOUNT;
PRINT '  Fines: ' + CAST(@Count AS VARCHAR(10)) + ' records';

-- Update Event
UPDATE Event SET SemesterId = @TargetSemesterId;
SET @Count = @@ROWCOUNT;
PRINT '  Event: ' + CAST(@Count AS VARCHAR(10)) + ' records';

-- Update Attendance
UPDATE Attendance SET SemesterId = @TargetSemesterId;
SET @Count = @@ROWCOUNT;
PRINT '  Attendance: ' + CAST(@Count AS VARCHAR(10)) + ' records';

-- Update PaymentTransactions
UPDATE PaymentTransactions SET SemesterId = @TargetSemesterId;
SET @Count = @@ROWCOUNT;
PRINT '  PaymentTransactions: ' + CAST(@Count AS VARCHAR(10)) + ' records';

-- Update Remittances (if column exists)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Remittances]') AND name = 'SemesterId')
BEGIN
    UPDATE Remittances SET SemesterId = @TargetSemesterId;
    SET @Count = @@ROWCOUNT;
    PRINT '  Remittances: ' + CAST(@Count AS VARCHAR(10)) + ' records';
END

-- Update Announcements (if column exists)
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Announcements]') AND name = 'SemesterId')
BEGIN
    UPDATE Announcements SET SemesterId = @TargetSemesterId;
    SET @Count = @@ROWCOUNT;
    PRINT '  Announcements: ' + CAST(@Count AS VARCHAR(10)) + ' records';
END

PRINT '';

-- =============================================
-- STEP 3: Delete StudentSemesters for other semesters
-- =============================================

PRINT 'Cleaning up StudentSemesters...';

DELETE FROM StudentSemesters WHERE SemesterId <> @TargetSemesterId;
SET @Count = @@ROWCOUNT;
PRINT '  Deleted ' + CAST(@Count AS VARCHAR(10)) + ' student enrollments from other semesters';

-- Re-enroll all students to target semester if needed
INSERT INTO StudentSemesters (StudentNum, SemesterId, YearLevel, IsActive)
SELECT DISTINCT 
    s.StudentNum,
    @TargetSemesterId,
    COALESCE(
        (SELECT TOP 1 YearLevel FROM StudentSemesters WHERE StudentNum = s.StudentNum ORDER BY StudentSemesterId DESC),
        1  -- Default to 1st year if no previous enrollment
    ),
    1
FROM Student s
WHERE NOT EXISTS (
    SELECT 1 FROM StudentSemesters ss 
    WHERE ss.StudentNum = s.StudentNum 
    AND ss.SemesterId = @TargetSemesterId
);

SET @Count = @@ROWCOUNT;
PRINT '  Enrolled ' + CAST(@Count AS VARCHAR(10)) + ' students to target semester';
PRINT '';

-- =============================================
-- STEP 4: Delete all other semesters
-- =============================================

PRINT 'Deleting other semesters...';

-- Get list of semesters to delete
DECLARE @SemestersToDelete TABLE (
    SemesterId INT,
    DisplayName NVARCHAR(100)
);

INSERT INTO @SemestersToDelete
SELECT 
    s.SemesterId,
    ay.YearName + ' - ' + s.SemesterName
FROM Semesters s
INNER JOIN AcademicYears ay ON s.AcademicYearId = ay.AcademicYearId
WHERE s.SemesterId <> @TargetSemesterId;

-- Show what will be deleted
SELECT * FROM @SemestersToDelete;

-- Delete them
DELETE FROM Semesters WHERE SemesterId <> @TargetSemesterId;
SET @Count = @@ROWCOUNT;
PRINT '  Deleted ' + CAST(@Count AS VARCHAR(10)) + ' semesters';
PRINT '';

-- =============================================
-- STEP 5: Clean up unused Academic Years
-- =============================================

PRINT 'Cleaning up unused Academic Years...';

DELETE FROM AcademicYears 
WHERE AcademicYearId NOT IN (SELECT AcademicYearId FROM Semesters);
SET @Count = @@ROWCOUNT;
PRINT '  Deleted ' + CAST(@Count AS VARCHAR(10)) + ' unused academic years';
PRINT '';

-- =============================================
-- STEP 6: Set target semester as current
-- =============================================

UPDATE Semesters SET IsCurrent = 0;
UPDATE Semesters SET IsCurrent = 1, IsActive = 1 WHERE SemesterId = @TargetSemesterId;

PRINT 'Set ' + @TargetAcademicYearName + ' - ' + @TargetSemesterName + ' as current semester';
PRINT '';

-- =============================================
-- FINAL VERIFICATION
-- =============================================

PRINT '========================================';
PRINT 'CLEANUP COMPLETED SUCCESSFULLY!';
PRINT '========================================';
PRINT '';

PRINT 'Final State:';
PRINT '  Remaining Semesters: 1';
PRINT '  Semester: ' + @TargetAcademicYearName + ' - ' + @TargetSemesterName;
PRINT '  Status: Current & Active';
PRINT '';

PRINT 'Record Counts:';
SELECT 
    'Fees' AS TableName, 
    COUNT(*) AS RecordCount,
    @TargetAcademicYearName + ' - ' + @TargetSemesterName AS Semester
FROM Fees
UNION ALL
SELECT 'Fines', COUNT(*), @TargetAcademicYearName + ' - ' + @TargetSemesterName FROM Fines
UNION ALL
SELECT 'Events', COUNT(*), @TargetAcademicYearName + ' - ' + @TargetSemesterName FROM Event
UNION ALL
SELECT 'Attendance', COUNT(*), @TargetAcademicYearName + ' - ' + @TargetSemesterName FROM Attendance
UNION ALL
SELECT 'Payments', COUNT(*), @TargetAcademicYearName + ' - ' + @TargetSemesterName FROM PaymentTransactions;

PRINT '';
PRINT 'All historical data removed. System is ready for fresh start!';
PRINT '========================================';

COMMIT TRANSACTION;
GO
