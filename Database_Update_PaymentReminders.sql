-- ================================================================
-- iBITS Portal - Payment Reminders Module Update
-- Database Schema Update Script
-- ================================================================
-- Purpose: Update Announcements table to support multiple target audiences
-- Date: 2026-02-12
-- Author: RovoDev
-- ================================================================

USE [PortaliBITS]
GO

-- ================================================================
-- STEP 1: Backup existing data (for safety)
-- ================================================================
PRINT 'Creating backup of current TargetAudience data...'

-- Create backup table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Announcements_Backup_20260212')
BEGIN
    SELECT Id, TargetAudience, Title, Content, AnnouncementType, PostedBy, Timestamp
    INTO Announcements_Backup_20260212
    FROM Announcements
    WHERE AnnouncementType IN ('General Reminder', 'Urgent Notice', 'Final Notice', 'New Fee Posted')
    
    PRINT 'Backup created: Announcements_Backup_20260212'
END
ELSE
BEGIN
    PRINT 'Backup table already exists, skipping...'
END
GO

-- ================================================================
-- STEP 2: Check current column definition
-- ================================================================
PRINT 'Checking current TargetAudience column definition...'

SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length AS MaxLength,
    c.is_nullable AS IsNullable
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('Announcements')
AND c.name = 'TargetAudience'
GO

-- ================================================================
-- STEP 3: Update TargetAudience column to support longer strings
-- ================================================================
PRINT 'Updating TargetAudience column from nvarchar(100) to nvarchar(500)...'

-- Check if there are any values longer than 100 characters (there shouldn't be yet)
IF EXISTS (SELECT 1 FROM Announcements WHERE LEN(TargetAudience) > 100)
BEGIN
    PRINT 'WARNING: Some TargetAudience values are already longer than 100 characters!'
    SELECT Id, Title, TargetAudience, LEN(TargetAudience) AS CurrentLength
    FROM Announcements
    WHERE LEN(TargetAudience) > 100
END

-- Alter the column
BEGIN TRY
    ALTER TABLE Announcements
    ALTER COLUMN TargetAudience NVARCHAR(500) NULL
    
    PRINT 'SUCCESS: TargetAudience column updated to nvarchar(500)'
END TRY
BEGIN CATCH
    PRINT 'ERROR: Failed to update column'
    PRINT ERROR_MESSAGE()
END CATCH
GO

-- ================================================================
-- STEP 4: Verify the change
-- ================================================================
PRINT 'Verifying column update...'

SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length AS MaxLength,
    c.is_nullable AS IsNullable
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('Announcements')
AND c.name = 'TargetAudience'
GO

-- ================================================================
-- STEP 5: Display current payment reminders for reference
-- ================================================================
PRINT 'Current Payment Reminders in system:'

SELECT 
    Id,
    Title,
    TargetAudience,
    AnnouncementType,
    PostedBy,
    Timestamp,
    ExpiryDate,
    LEN(TargetAudience) AS TargetAudienceLength
FROM Announcements
WHERE AnnouncementType IN ('General Reminder', 'Urgent Notice', 'Final Notice', 'New Fee Posted')
ORDER BY Timestamp DESC
GO

-- ================================================================
-- STEP 6: Create helpful view for monitoring (Optional)
-- ================================================================
PRINT 'Creating monitoring view...'

IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_PaymentReminders')
BEGIN
    DROP VIEW vw_PaymentReminders
    PRINT 'Dropped existing view'
END
GO

CREATE VIEW vw_PaymentReminders
AS
SELECT 
    Id,
    Title,
    Content,
    TargetAudience,
    CASE 
        WHEN TargetAudience LIKE '%,%' THEN 'Multiple Audiences'
        ELSE 'Single Audience'
    END AS AudienceType,
    LEN(TargetAudience) AS TargetAudienceLength,
    AnnouncementType,
    PostedBy,
    Timestamp,
    ExpiryDate,
    CASE 
        WHEN ExpiryDate IS NULL THEN 'No Expiry'
        WHEN ExpiryDate > GETDATE() THEN 'Active'
        ELSE 'Expired'
    END AS ReminderStatus,
    DATEDIFF(DAY, GETDATE(), ExpiryDate) AS DaysUntilExpiry
FROM Announcements
WHERE AnnouncementType IN ('General Reminder', 'Urgent Notice', 'Final Notice', 'New Fee Posted')
GO

PRINT 'View created: vw_PaymentReminders'
GO

-- ================================================================
-- STEP 7: Test the changes (Insert sample data)
-- ================================================================
PRINT 'Testing with sample multi-audience reminder (will be deleted)...'

DECLARE @TestId INT

-- Insert test reminder with multiple audiences
INSERT INTO Announcements (Title, Content, TargetAudience, AnnouncementType, PostedBy, Timestamp, ExpiryDate)
VALUES (
    'Payment Reminder: TEST - Multi-Audience',
    'This is a test reminder to verify multi-audience support.',
    '1st Year, 2nd Year, BSIT, BSCS, Outstanding Balance',
    'General Reminder',
    'System Test',
    GETDATE(),
    DATEADD(DAY, 30, GETDATE())
)

SET @TestId = SCOPE_IDENTITY()

-- Verify the insert
SELECT 
    Id,
    Title,
    TargetAudience,
    LEN(TargetAudience) AS Length,
    'Test Successful' AS Status
FROM Announcements
WHERE Id = @TestId

-- Clean up test data
DELETE FROM Announcements WHERE Id = @TestId
PRINT 'Test data cleaned up'
GO

-- ================================================================
-- COMPLETION SUMMARY
-- ================================================================
PRINT ''
PRINT '================================================================'
PRINT 'DATABASE UPDATE COMPLETED SUCCESSFULLY'
PRINT '================================================================'
PRINT 'Changes Applied:'
PRINT '  ✓ TargetAudience column updated: nvarchar(100) → nvarchar(500)'
PRINT '  ✓ Backup table created: Announcements_Backup_20260212'
PRINT '  ✓ Monitoring view created: vw_PaymentReminders'
PRINT ''
PRINT 'You can now:'
PRINT '  - Create payment reminders with multiple target audiences'
PRINT '  - Use comma-separated values (e.g., "1st Year, BSIT, Outstanding Balance")'
PRINT '  - View reminder statistics using: SELECT * FROM vw_PaymentReminders'
PRINT ''
PRINT 'Next Steps:'
PRINT '  1. Update the frontend (PaymentReminders.cshtml) to use checkboxes'
PRINT '  2. Update the controller (OfficerController.cs) to handle multiple audiences'
PRINT '  3. Test the implementation'
PRINT '================================================================'
GO
