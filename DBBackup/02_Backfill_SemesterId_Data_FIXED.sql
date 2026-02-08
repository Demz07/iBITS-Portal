-- =============================================
-- Semester Historical Records: Backfill Data (FIXED)
-- Script 2 of 3 - SAFE VERSION
-- Only updates tables that ACTUALLY have SemesterId column
-- =============================================

USE [PortaliBITS]  -- CORRECTED DATABASE NAME
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
PRINT 'Backfilling SemesterId Data (SAFE MODE)';
PRINT '========================================';
PRINT 'Current Semester ID: ' + CAST(@CurrentSemesterId AS VARCHAR(10));
PRINT 'Starting data migration...';
PRINT '';

-- =============================================
-- Update Fees (if SemesterId column exists)
-- =============================================
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Fees]') AND name = 'SemesterId')
BEGIN
    UPDATE Fees 
    SET SemesterId = @CurrentSemesterId 
    WHERE SemesterId IS NULL;
    SET @RecordsUpdated = @@ROWCOUNT;
    SET @TotalUpdated = @TotalUpdated + @RecordsUpdated;
    PRINT 'Fees: Updated ' + CAST(@RecordsUpdated AS VARCHAR(10)) + ' records';
END
ELSE
BEGIN
    PRINT 'Fees: Skipped (no SemesterId column)';
END

-- =============================================
-- Update Fines (if SemesterId column exists)
-- =============================================
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Fines]') AND name = 'SemesterId')
BEGIN
    UPDATE Fines 
    SET SemesterId = @CurrentSemesterId 
    WHERE SemesterId IS NULL;
    SET @RecordsUpdated = @@ROWCOUNT;
    SET @TotalUpdated = @TotalUpdated + @RecordsUpdated;
    PRINT 'Fines: Updated ' + CAST(@RecordsUpdated AS VARCHAR(10)) + ' records';
END
ELSE
BEGIN
    PRINT 'Fines: Skipped (no SemesterId column)';
END

-- =============================================
-- Update Event (if SemesterId column exists)
-- =============================================
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Event]') AND name = 'SemesterId')
BEGIN
    UPDATE Event 
    SET SemesterId = @CurrentSemesterId 
    WHERE SemesterId IS NULL;
    SET @RecordsUpdated = @@ROWCOUNT;
    SET @TotalUpdated = @TotalUpdated + @RecordsUpdated;
    PRINT 'Event: Updated ' + CAST(@RecordsUpdated AS VARCHAR(10)) + ' records';
END
ELSE
BEGIN
    PRINT 'Event: Skipped (no SemesterId column)';
END

-- =============================================
-- Update Attendance (if SemesterId column exists)
-- =============================================
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Attendance]') AND name = 'SemesterId')
BEGIN
    UPDATE Attendance 
    SET SemesterId = @CurrentSemesterId 
    WHERE SemesterId IS NULL;
    SET @RecordsUpdated = @@ROWCOUNT;
    SET @TotalUpdated = @TotalUpdated + @RecordsUpdated;
    PRINT 'Attendance: Updated ' + CAST(@RecordsUpdated AS VARCHAR(10)) + ' records';
END
ELSE
BEGIN
    PRINT 'Attendance: Skipped (no SemesterId column)';
END

-- =============================================
-- Update PaymentTransactions (if SemesterId column exists)
-- =============================================
IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]') AND name = 'SemesterId')
BEGIN
    UPDATE PaymentTransactions 
    SET SemesterId = @CurrentSemesterId 
    WHERE SemesterId IS NULL;
    SET @RecordsUpdated = @@ROWCOUNT;
    SET @TotalUpdated = @TotalUpdated + @RecordsUpdated;
    PRINT 'PaymentTransactions: Updated ' + CAST(@RecordsUpdated AS VARCHAR(10)) + ' records';
END
ELSE
BEGIN
    PRINT 'PaymentTransactions: Skipped (no SemesterId column)';
END

-- =============================================
-- Update Announcements (if table and column exist)
-- =============================================
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Announcements')
BEGIN
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Announcements]') AND name = 'SemesterId')
    BEGIN
        UPDATE Announcements 
        SET SemesterId = @CurrentSemesterId 
        WHERE SemesterId IS NULL;
        SET @RecordsUpdated = @@ROWCOUNT;
        SET @TotalUpdated = @TotalUpdated + @RecordsUpdated;
        PRINT 'Announcements: Updated ' + CAST(@RecordsUpdated AS VARCHAR(10)) + ' records';
    END
    ELSE
    BEGIN
        PRINT 'Announcements: Skipped (no SemesterId column)';
    END
END
ELSE
BEGIN
    PRINT 'Announcements: Skipped (table does not exist)';
END

-- =============================================
-- Update Remittances (if table and column exist)
-- =============================================
IF EXISTS (SELECT * FROM sys.tables WHERE name = 'Remittances')
BEGIN
    IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Remittances]') AND name = 'SemesterId')
    BEGIN
        UPDATE Remittances 
        SET SemesterId = @CurrentSemesterId 
        WHERE SemesterId IS NULL;
        SET @RecordsUpdated = @@ROWCOUNT;
        SET @TotalUpdated = @TotalUpdated + @RecordsUpdated;
        PRINT 'Remittances: Updated ' + CAST(@RecordsUpdated AS VARCHAR(10)) + ' records';
    END
    ELSE
    BEGIN
        PRINT 'Remittances: Skipped (no SemesterId column)';
    END
END
ELSE
BEGIN
    PRINT 'Remittances: Skipped (table does not exist)';
END

PRINT '';
PRINT '========================================';
PRINT 'Total Records Updated: ' + CAST(@TotalUpdated AS VARCHAR(10));
PRINT 'Backfill completed successfully!';
PRINT '========================================';
PRINT '';
PRINT 'Next: (Optional) Run 03_Make_SemesterId_Required.sql';
PRINT '========================================';

COMMIT TRANSACTION;
GO
