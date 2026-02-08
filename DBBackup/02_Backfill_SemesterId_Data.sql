-- =============================================
-- Semester Historical Records: Backfill Data
-- Script 2 of 3
-- =============================================

USE [iBITSPortal]
GO

BEGIN TRANSACTION;

DECLARE @CurrentSemesterId INT;
DECLARE @RecordsUpdated INT = 0;
DECLARE @TotalUpdated INT = 0;

-- Get current semester ID
SELECT @CurrentSemesterId = SemesterId 
FROM Semesters 
WHERE IsCurrent = 1 AND IsActive = 1;

-- Validate current semester exists
IF @CurrentSemesterId IS NULL
BEGIN
    PRINT 'ERROR: No current semester found!';
    PRINT 'Please create a current semester before running this migration.';
    PRINT 'Go to Admin > Semesters and set one semester as current.';
    ROLLBACK TRANSACTION;
    RETURN;
END

PRINT '========================================';
PRINT 'Backfilling SemesterId Data';
PRINT '========================================';
PRINT 'Current Semester ID: ' + CAST(@CurrentSemesterId AS VARCHAR(10));
PRINT 'Starting data migration...';
PRINT '';

-- Update Fees without semester assignment
UPDATE Fees 
SET SemesterId = @CurrentSemesterId 
WHERE SemesterId IS NULL;
SET @RecordsUpdated = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @RecordsUpdated;
PRINT 'Fees: Updated ' + CAST(@RecordsUpdated AS VARCHAR(10)) + ' records';

-- Update Fines without semester assignment
UPDATE Fines 
SET SemesterId = @CurrentSemesterId 
WHERE SemesterId IS NULL;
SET @RecordsUpdated = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @RecordsUpdated;
PRINT 'Fines: Updated ' + CAST(@RecordsUpdated AS VARCHAR(10)) + ' records';

-- Update Events without semester assignment
UPDATE Events 
SET SemesterId = @CurrentSemesterId 
WHERE SemesterId IS NULL;
SET @RecordsUpdated = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @RecordsUpdated;
PRINT 'Events: Updated ' + CAST(@RecordsUpdated AS VARCHAR(10)) + ' records';

-- Update Attendances without semester assignment
UPDATE Attendances 
SET SemesterId = @CurrentSemesterId 
WHERE SemesterId IS NULL;
SET @RecordsUpdated = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @RecordsUpdated;
PRINT 'Attendances: Updated ' + CAST(@RecordsUpdated AS VARCHAR(10)) + ' records';

-- Update PaymentTransactions without semester assignment
UPDATE PaymentTransactions 
SET SemesterId = @CurrentSemesterId 
WHERE SemesterId IS NULL;
SET @RecordsUpdated = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @RecordsUpdated;
PRINT 'PaymentTransactions: Updated ' + CAST(@RecordsUpdated AS VARCHAR(10)) + ' records';

-- Update Announcements without semester assignment
UPDATE Announcements 
SET SemesterId = @CurrentSemesterId 
WHERE SemesterId IS NULL;
SET @RecordsUpdated = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @RecordsUpdated;
PRINT 'Announcements: Updated ' + CAST(@RecordsUpdated AS VARCHAR(10)) + ' records';

-- Update Remittances without semester assignment
UPDATE Remittances 
SET SemesterId = @CurrentSemesterId 
WHERE SemesterId IS NULL;
SET @RecordsUpdated = @@ROWCOUNT;
SET @TotalUpdated = @TotalUpdated + @RecordsUpdated;
PRINT 'Remittances: Updated ' + CAST(@RecordsUpdated AS VARCHAR(10)) + ' records';

PRINT '';
PRINT '========================================';
PRINT 'Total Records Updated: ' + CAST(@TotalUpdated AS VARCHAR(10));
PRINT 'Script 2 completed successfully!';
PRINT 'Next: Run 03_Make_SemesterId_Required.sql (OPTIONAL)';
PRINT '========================================';

COMMIT TRANSACTION;
GO
