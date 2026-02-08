-- =============================================
-- ROLLBACK: Delete Test Semester & Restore Current
-- Use this to delete newly created test semesters
-- =============================================

USE [PortaliBITS]
GO

-- =============================================
-- INSTRUCTIONS:
-- 1. Find the semester you want to delete (look at the list below)
-- 2. Update @SemesterIdToDelete with the ID you want to remove
-- 3. Update @RestoreCurrentSemesterId with the semester to make current again
-- 4. Run this script
-- =============================================

-- CHANGE THESE VALUES:
DECLARE @SemesterIdToDelete INT = 14;  -- The test semester you want to delete
DECLARE @RestoreCurrentSemesterId INT = 13;  -- The semester to make current again

-- =============================================
-- DO NOT MODIFY BELOW THIS LINE
-- =============================================

BEGIN TRANSACTION;

PRINT '========================================';
PRINT 'ROLLBACK: DELETE TEST SEMESTER';
PRINT '========================================';
PRINT '';

-- Show all semesters first
PRINT 'Current Semesters in Database:';
PRINT '';
SELECT 
    s.SemesterId,
    ay.YearName + ' - ' + s.SemesterName AS 'Semester',
    CASE WHEN s.IsCurrent = 1 THEN 'CURRENT' ELSE 'Historical' END AS 'Status',
    s.StartDate,
    s.EndDate
FROM Semesters s
INNER JOIN AcademicYears ay ON s.AcademicYearId = ay.AcademicYearId
ORDER BY s.StartDate DESC;

PRINT '';
PRINT '========================================';
PRINT '';

-- Validate semester to delete exists
IF NOT EXISTS (SELECT * FROM Semesters WHERE SemesterId = @SemesterIdToDelete)
BEGIN
    PRINT 'ERROR: Semester ID ' + CAST(@SemesterIdToDelete AS VARCHAR(10)) + ' does not exist!';
    PRINT 'Check the list above and update @SemesterIdToDelete';
    ROLLBACK TRANSACTION;
    RETURN;
END

-- Validate restore semester exists
IF NOT EXISTS (SELECT * FROM Semesters WHERE SemesterId = @RestoreCurrentSemesterId)
BEGIN
    PRINT 'ERROR: Semester ID ' + CAST(@RestoreCurrentSemesterId AS VARCHAR(10)) + ' does not exist!';
    PRINT 'Check the list above and update @RestoreCurrentSemesterId';
    ROLLBACK TRANSACTION;
    RETURN;
END

-- Get semester info
DECLARE @DeleteSemesterName NVARCHAR(100);
DECLARE @RestoreSemesterName NVARCHAR(100);

SELECT @DeleteSemesterName = ay.YearName + ' - ' + s.SemesterName
FROM Semesters s
INNER JOIN AcademicYears ay ON s.AcademicYearId = ay.AcademicYearId
WHERE s.SemesterId = @SemesterIdToDelete;

SELECT @RestoreSemesterName = ay.YearName + ' - ' + s.SemesterName
FROM Semesters s
INNER JOIN AcademicYears ay ON s.AcademicYearId = ay.AcademicYearId
WHERE s.SemesterId = @RestoreCurrentSemesterId;

PRINT 'Will DELETE: ' + @DeleteSemesterName + ' (ID: ' + CAST(@SemesterIdToDelete AS VARCHAR(10)) + ')';
PRINT 'Will RESTORE as current: ' + @RestoreSemesterName + ' (ID: ' + CAST(@RestoreCurrentSemesterId AS VARCHAR(10)) + ')';
PRINT '';

-- Check if semester has any records
DECLARE @RecordCount INT = 0;

SELECT @RecordCount = 
    (SELECT COUNT(*) FROM Fees WHERE SemesterId = @SemesterIdToDelete) +
    (SELECT COUNT(*) FROM Fines WHERE SemesterId = @SemesterIdToDelete) +
    (SELECT COUNT(*) FROM Event WHERE SemesterId = @SemesterIdToDelete) +
    (SELECT COUNT(*) FROM Attendance WHERE SemesterId = @SemesterIdToDelete) +
    (SELECT COUNT(*) FROM PaymentTransactions WHERE SemesterId = @SemesterIdToDelete);

IF @RecordCount > 0
BEGIN
    PRINT 'WARNING: This semester has ' + CAST(@RecordCount AS VARCHAR(10)) + ' records!';
    PRINT '';
    PRINT 'Records will be moved to Semester ' + CAST(@RestoreCurrentSemesterId AS VARCHAR(10)) + ' (' + @RestoreSemesterName + ')';
    PRINT '';
    
    -- Move records to restore semester
    PRINT 'Moving records...';
    
    UPDATE Fees SET SemesterId = @RestoreCurrentSemesterId WHERE SemesterId = @SemesterIdToDelete;
    PRINT '  Fees: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' records moved';
    
    UPDATE Fines SET SemesterId = @RestoreCurrentSemesterId WHERE SemesterId = @SemesterIdToDelete;
    PRINT '  Fines: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' records moved';
    
    UPDATE Event SET SemesterId = @RestoreCurrentSemesterId WHERE SemesterId = @SemesterIdToDelete;
    PRINT '  Event: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' records moved';
    
    UPDATE Attendance SET SemesterId = @RestoreCurrentSemesterId WHERE SemesterId = @SemesterIdToDelete;
    PRINT '  Attendance: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' records moved';
    
    UPDATE PaymentTransactions SET SemesterId = @RestoreCurrentSemesterId WHERE SemesterId = @SemesterIdToDelete;
    PRINT '  PaymentTransactions: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' records moved';
    
    -- Move Remittances if exists
    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Remittances')
    BEGIN
        IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Remittances]') AND name = 'SemesterId')
        BEGIN
            UPDATE Remittances SET SemesterId = @RestoreCurrentSemesterId WHERE SemesterId = @SemesterIdToDelete;
            PRINT '  Remittances: ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' records moved';
        END
    END
    
    PRINT '';
END
ELSE
BEGIN
    PRINT 'Semester has NO records - safe to delete';
    PRINT '';
END

-- Delete the semester
DELETE FROM Semesters WHERE SemesterId = @SemesterIdToDelete;
PRINT '✓ Deleted semester: ' + @DeleteSemesterName;
PRINT '';

-- Restore current semester
UPDATE Semesters SET IsCurrent = 0;
UPDATE Semesters SET IsCurrent = 1 WHERE SemesterId = @RestoreCurrentSemesterId;
PRINT '✓ Restored current semester: ' + @RestoreSemesterName;
PRINT '';

PRINT '========================================';
PRINT 'ROLLBACK COMPLETED SUCCESSFULLY!';
PRINT '========================================';
PRINT '';
PRINT 'Summary:';
PRINT '  - Deleted: ' + @DeleteSemesterName;
PRINT '  - Current: ' + @RestoreSemesterName;
IF @RecordCount > 0
BEGIN
    PRINT '  - Moved: ' + CAST(@RecordCount AS VARCHAR(10)) + ' records';
END
PRINT '';
PRINT 'Your database is back to the state before the test.';
PRINT '========================================';

COMMIT TRANSACTION;
GO
