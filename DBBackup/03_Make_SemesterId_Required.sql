-- =============================================
-- Semester Historical Records: Make SemesterId Required
-- Script 3 of 3 (OPTIONAL - Run only after backfill is verified)
-- =============================================

USE [iBITSPortal]
GO

BEGIN TRANSACTION;

PRINT '========================================';
PRINT 'Making SemesterId columns NOT NULL';
PRINT 'WARNING: This is irreversible!';
PRINT '========================================';
PRINT '';

-- Check for any remaining NULL values
DECLARE @NullCount INT;

SELECT @NullCount = COUNT(*) FROM Fees WHERE SemesterId IS NULL;
IF @NullCount > 0
BEGIN
    PRINT 'ERROR: Found ' + CAST(@NullCount AS VARCHAR(10)) + ' NULL values in Fees.SemesterId';
    PRINT 'Run Script 2 (Backfill) first!';
    ROLLBACK TRANSACTION;
    RETURN;
END

SELECT @NullCount = COUNT(*) FROM Fines WHERE SemesterId IS NULL;
IF @NullCount > 0
BEGIN
    PRINT 'ERROR: Found ' + CAST(@NullCount AS VARCHAR(10)) + ' NULL values in Fines.SemesterId';
    ROLLBACK TRANSACTION;
    RETURN;
END

SELECT @NullCount = COUNT(*) FROM Events WHERE SemesterId IS NULL;
IF @NullCount > 0
BEGIN
    PRINT 'ERROR: Found ' + CAST(@NullCount AS VARCHAR(10)) + ' NULL values in Events.SemesterId';
    ROLLBACK TRANSACTION;
    RETURN;
END

SELECT @NullCount = COUNT(*) FROM Attendances WHERE SemesterId IS NULL;
IF @NullCount > 0
BEGIN
    PRINT 'ERROR: Found ' + CAST(@NullCount AS VARCHAR(10)) + ' NULL values in Attendances.SemesterId';
    ROLLBACK TRANSACTION;
    RETURN;
END

SELECT @NullCount = COUNT(*) FROM PaymentTransactions WHERE SemesterId IS NULL;
IF @NullCount > 0
BEGIN
    PRINT 'ERROR: Found ' + CAST(@NullCount AS VARCHAR(10)) + ' NULL values in PaymentTransactions.SemesterId';
    ROLLBACK TRANSACTION;
    RETURN;
END

SELECT @NullCount = COUNT(*) FROM Announcements WHERE SemesterId IS NULL;
IF @NullCount > 0
BEGIN
    PRINT 'ERROR: Found ' + CAST(@NullCount AS VARCHAR(10)) + ' NULL values in Announcements.SemesterId';
    ROLLBACK TRANSACTION;
    RETURN;
END

SELECT @NullCount = COUNT(*) FROM Remittances WHERE SemesterId IS NULL;
IF @NullCount > 0
BEGIN
    PRINT 'ERROR: Found ' + CAST(@NullCount AS VARCHAR(10)) + ' NULL values in Remittances.SemesterId';
    ROLLBACK TRANSACTION;
    RETURN;
END

PRINT 'All tables verified - No NULL values found';
PRINT 'Proceeding with ALTER TABLE statements...';
PRINT '';

-- Make SemesterId NOT NULL
ALTER TABLE Fees 
ALTER COLUMN SemesterId INT NOT NULL;
PRINT 'Fees.SemesterId is now required';

ALTER TABLE Fines 
ALTER COLUMN SemesterId INT NOT NULL;
PRINT 'Fines.SemesterId is now required';

ALTER TABLE Events 
ALTER COLUMN SemesterId INT NOT NULL;
PRINT 'Events.SemesterId is now required';

ALTER TABLE Attendances 
ALTER COLUMN SemesterId INT NOT NULL;
PRINT 'Attendances.SemesterId is now required';

ALTER TABLE PaymentTransactions 
ALTER COLUMN SemesterId INT NOT NULL;
PRINT 'PaymentTransactions.SemesterId is now required';

ALTER TABLE Announcements 
ALTER COLUMN SemesterId INT NOT NULL;
PRINT 'Announcements.SemesterId is now required';

ALTER TABLE Remittances 
ALTER COLUMN SemesterId INT NOT NULL;
PRINT 'Remittances.SemesterId is now required';

PRINT '';
PRINT '========================================';
PRINT 'All SemesterId columns are now required (NOT NULL)';
PRINT 'Migration completed successfully!';
PRINT '========================================';

COMMIT TRANSACTION;
GO
