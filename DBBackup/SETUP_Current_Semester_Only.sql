-- =============================================
-- SETUP: Current Semester Only (No Historical Viewing)
-- Step 1: Ensure all records are in ONE current semester
-- Step 2: Create ability to start fresh new semester
-- =============================================

USE [PortaliBITS]
GO

BEGIN TRANSACTION;

PRINT '========================================';
PRINT 'CURRENT SEMESTER SETUP';
PRINT '========================================';
PRINT '';

-- =============================================
-- STEP 1: Find or Create Current Semester
-- =============================================
DECLARE @CurrentSemesterId INT;
DECLARE @RecordsUpdated INT = 0;

-- Check if current semester exists
SELECT @CurrentSemesterId = SemesterId 
FROM Semesters 
WHERE IsCurrent = 1 AND IsActive = 1;

IF @CurrentSemesterId IS NULL
BEGIN
    PRINT 'WARNING: No current semester found!';
    PRINT '';
    PRINT 'Please create a current semester first:';
    PRINT '  1. Go to Admin > Semesters';
    PRINT '  2. Create a new Semester';
    PRINT '  3. Set it as "Current"';
    PRINT '';
    ROLLBACK TRANSACTION;
    RETURN;
END

PRINT 'Current Semester ID: ' + CAST(@CurrentSemesterId AS VARCHAR(10));

-- Get semester details
DECLARE @SemesterName NVARCHAR(50);
DECLARE @AcademicYearName NVARCHAR(20);

SELECT 
    @SemesterName = s.SemesterName,
    @AcademicYearName = ay.YearName
FROM Semesters s
INNER JOIN AcademicYears ay ON s.AcademicYearId = ay.AcademicYearId
WHERE s.SemesterId = @CurrentSemesterId;

PRINT 'Current Semester: ' + @AcademicYearName + ' - ' + @SemesterName;
PRINT '';

-- =============================================
-- STEP 2: Move ALL records to current semester
-- =============================================
PRINT 'Moving ALL records to current semester...';
PRINT '';

-- Update Fees
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Fees]') AND name = 'SemesterId')
BEGIN
    UPDATE Fees SET SemesterId = @CurrentSemesterId;
    SET @RecordsUpdated = @@ROWCOUNT;
    PRINT 'Fees: ' + CAST(@RecordsUpdated AS VARCHAR(10)) + ' records → Semester ' + CAST(@CurrentSemesterId AS VARCHAR(10));
END

-- Update Fines
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Fines]') AND name = 'SemesterId')
BEGIN
    UPDATE Fines SET SemesterId = @CurrentSemesterId;
    SET @RecordsUpdated = @@ROWCOUNT;
    PRINT 'Fines: ' + CAST(@RecordsUpdated AS VARCHAR(10)) + ' records → Semester ' + CAST(@CurrentSemesterId AS VARCHAR(10));
END

-- Update Event
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Event]') AND name = 'SemesterId')
BEGIN
    UPDATE Event SET SemesterId = @CurrentSemesterId;
    SET @RecordsUpdated = @@ROWCOUNT;
    PRINT 'Event: ' + CAST(@RecordsUpdated AS VARCHAR(10)) + ' records → Semester ' + CAST(@CurrentSemesterId AS VARCHAR(10));
END

-- Update Attendance
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Attendance]') AND name = 'SemesterId')
BEGIN
    UPDATE Attendance SET SemesterId = @CurrentSemesterId;
    SET @RecordsUpdated = @@ROWCOUNT;
    PRINT 'Attendance: ' + CAST(@RecordsUpdated AS VARCHAR(10)) + ' records → Semester ' + CAST(@CurrentSemesterId AS VARCHAR(10));
END

-- Update PaymentTransactions
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]') AND name = 'SemesterId')
BEGIN
    UPDATE PaymentTransactions SET SemesterId = @CurrentSemesterId;
    SET @RecordsUpdated = @@ROWCOUNT;
    PRINT 'PaymentTransactions: ' + CAST(@RecordsUpdated AS VARCHAR(10)) + ' records → Semester ' + CAST(@CurrentSemesterId AS VARCHAR(10));
END

-- Update Remittances (if exists)
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Remittances')
BEGIN
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Remittances]') AND name = 'SemesterId')
    BEGIN
        UPDATE Remittances SET SemesterId = @CurrentSemesterId;
        SET @RecordsUpdated = @@ROWCOUNT;
        PRINT 'Remittances: ' + CAST(@RecordsUpdated AS VARCHAR(10)) + ' records → Semester ' + CAST(@CurrentSemesterId AS VARCHAR(10));
    END
END

-- Update Announcements (if exists)
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Announcements')
BEGIN
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Announcements]') AND name = 'SemesterId')
    BEGIN
        UPDATE Announcements SET SemesterId = @CurrentSemesterId;
        SET @RecordsUpdated = @@ROWCOUNT;
        PRINT 'Announcements: ' + CAST(@RecordsUpdated AS VARCHAR(10)) + ' records → Semester ' + CAST(@CurrentSemesterId AS VARCHAR(10));
    END
END

PRINT '';
PRINT '========================================';
PRINT 'ALL RECORDS NOW IN CURRENT SEMESTER!';
PRINT '========================================';
PRINT '';
PRINT 'Current Semester: ' + @AcademicYearName + ' - ' + @SemesterName + ' (ID: ' + CAST(@CurrentSemesterId AS VARCHAR(10)) + ')';
PRINT '';
PRINT 'Next Steps:';
PRINT '  1. Run: DISABLE_Historical_Viewing.sql';
PRINT '     (This prevents users from viewing old semesters)';
PRINT '';
PRINT '  2. When ready for new semester:';
PRINT '     Run: CREATE_New_Fresh_Semester.sql';
PRINT '========================================';

COMMIT TRANSACTION;
GO
