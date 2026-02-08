-- =============================================
-- FINAL SETUP: Move ALL Records to Current Semester
-- Run this to ensure everything is in one semester
-- =============================================

USE [PortaliBITS]
GO

BEGIN TRANSACTION;

DECLARE @CurrentSemesterId INT;

-- Get current semester
SELECT @CurrentSemesterId = SemesterId 
FROM Semesters 
WHERE IsCurrent = 1 AND IsActive = 1;

-- If no current semester, fail
IF @CurrentSemesterId IS NULL
BEGIN
    PRINT 'ERROR: No current semester found!';
    PRINT 'Please go to Admin > Semesters and set one as current.';
    ROLLBACK TRANSACTION;
    RETURN;
END

PRINT '========================================';
PRINT 'MOVING ALL RECORDS TO CURRENT SEMESTER';
PRINT '========================================';
PRINT '';
PRINT 'Current Semester ID: ' + CAST(@CurrentSemesterId AS VARCHAR(10));

-- Get semester name
DECLARE @SemesterInfo NVARCHAR(100);
SELECT @SemesterInfo = ay.YearName + ' - ' + s.SemesterName
FROM Semesters s
INNER JOIN AcademicYears ay ON s.AcademicYearId = ay.AcademicYearId
WHERE s.SemesterId = @CurrentSemesterId;

PRINT 'Current Semester: ' + @SemesterInfo;
PRINT '';
PRINT 'Moving records...';
PRINT '';

DECLARE @Count INT;

-- Update Fees
UPDATE Fees SET SemesterId = @CurrentSemesterId;
SET @Count = @@ROWCOUNT;
PRINT 'Fees: ' + CAST(@Count AS VARCHAR(10)) + ' records';

-- Update Fines
UPDATE Fines SET SemesterId = @CurrentSemesterId;
SET @Count = @@ROWCOUNT;
PRINT 'Fines: ' + CAST(@Count AS VARCHAR(10)) + ' records';

-- Update Event
UPDATE Event SET SemesterId = @CurrentSemesterId;
SET @Count = @@ROWCOUNT;
PRINT 'Event: ' + CAST(@Count AS VARCHAR(10)) + ' records';

-- Update Attendance
UPDATE Attendance SET SemesterId = @CurrentSemesterId;
SET @Count = @@ROWCOUNT;
PRINT 'Attendance: ' + CAST(@Count AS VARCHAR(10)) + ' records';

-- Update PaymentTransactions
UPDATE PaymentTransactions SET SemesterId = @CurrentSemesterId;
SET @Count = @@ROWCOUNT;
PRINT 'PaymentTransactions: ' + CAST(@Count AS VARCHAR(10)) + ' records';

-- Update Remittances if exists
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Remittances')
BEGIN
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Remittances]') AND name = 'SemesterId')
    BEGIN
        UPDATE Remittances SET SemesterId = @CurrentSemesterId;
        SET @Count = @@ROWCOUNT;
        PRINT 'Remittances: ' + CAST(@Count AS VARCHAR(10)) + ' records';
    END
END

PRINT '';
PRINT '========================================';
PRINT 'SUCCESS!';
PRINT '========================================';
PRINT '';
PRINT 'All records are now in: ' + @SemesterInfo;
PRINT '';
PRINT 'What this means:';
PRINT '  - No historical data (everything in current semester)';
PRINT '  - When you create a NEW semester via dropdown:';
PRINT '    * Old records stay here (become historical)';
PRINT '    * New semester starts fresh (empty)';
PRINT '  - You can view historical semesters via dropdown';
PRINT '';
PRINT '========================================';

COMMIT TRANSACTION;
GO
