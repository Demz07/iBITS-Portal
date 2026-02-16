-- =============================================
-- DELETE ALL EVENTS AND RESET IDENTITY TO 1
-- iBITS Portal - Event Cleanup Script
-- =============================================
-- This script will:
-- 1. Delete all related data (Attendance, Fines, Archived Events)
-- 2. Delete all Events
-- 3. Reset the EventId identity counter back to 1
-- =============================================

USE PortaliBITS;
GO

BEGIN TRANSACTION;

BEGIN TRY
    PRINT '========================================';
    PRINT 'Starting Event Deletion Process...';
    PRINT '========================================';
    PRINT '';

    -- Step 1: Count existing records before deletion
    DECLARE @EventCount INT;
    DECLARE @AttendanceCount INT;
    DECLARE @FineCount INT;
    DECLARE @ArchivedEventCount INT;

    SELECT @EventCount = COUNT(*) FROM Event;
    SELECT @AttendanceCount = COUNT(*) FROM Attendance;
    SELECT @FineCount = COUNT(*) FROM Fines WHERE AttendanceId IS NOT NULL;
    SELECT @ArchivedEventCount = COUNT(*) FROM ArchivedEvents;

    PRINT 'BEFORE DELETION:';
    PRINT '  - Events: ' + CAST(@EventCount AS VARCHAR(10));
    PRINT '  - Attendance Records: ' + CAST(@AttendanceCount AS VARCHAR(10));
    PRINT '  - Event-Related Fines: ' + CAST(@FineCount AS VARCHAR(10));
    PRINT '  - Archived Events: ' + CAST(@ArchivedEventCount AS VARCHAR(10));
    PRINT '';

    -- Step 2: Unlink Fines from Attendance (set to NULL instead of deleting fines)
    PRINT 'Step 1/5: Unlinking Fines from Attendance...';
    UPDATE Fines 
    SET AttendanceId = NULL 
    WHERE AttendanceId IS NOT NULL;
    PRINT '  ✓ Unlinked ' + CAST(@FineCount AS VARCHAR(10)) + ' fine(s) from attendance/events';
    PRINT '';

    -- Step 3: Delete Attendance records (references Event)
    PRINT 'Step 2/5: Deleting Attendance records...';
    DELETE FROM Attendance;
    PRINT '  ✓ Deleted ' + CAST(@AttendanceCount AS VARCHAR(10)) + ' attendance record(s)';
    PRINT '';

    -- Step 4: Delete Archived Events
    PRINT 'Step 3/5: Deleting Archived Events...';
    DELETE FROM ArchivedEvents;
    PRINT '  ✓ Deleted ' + CAST(@ArchivedEventCount AS VARCHAR(10)) + ' archived event(s)';
    PRINT '';

    -- Step 5: Delete all Events
    PRINT 'Step 4/5: Deleting all Events...';
    DELETE FROM Event;
    PRINT '  ✓ Deleted ' + CAST(@EventCount AS VARCHAR(10)) + ' event(s)';
    PRINT '';

    -- Step 6: Reset Identity to 1
    PRINT 'Step 5/5: Resetting EventId identity to 1...';
    DBCC CHECKIDENT ('Event', RESEED, 0);
    PRINT '  ✓ EventId identity reset to 1';
    PRINT '';

    -- Step 7: Reset Attendance Identity to 1
    PRINT 'Step 6/6: Resetting AttendanceId identity to 1...';
    DBCC CHECKIDENT ('Attendance', RESEED, 0);
    PRINT '  ✓ AttendanceId identity reset to 1';
    PRINT '';

    -- Verify the reset
    DECLARE @CurrentIdentity INT;
    SELECT @CurrentIdentity = IDENT_CURRENT('Event');
    
    PRINT '========================================';
    PRINT 'DELETION COMPLETED SUCCESSFULLY!';
    PRINT '========================================';
    PRINT 'AFTER DELETION:';
    PRINT '  - Events: 0';
    PRINT '  - Attendance Records: 0';
    PRINT '  - Event-Related Fines: Unlinked (not deleted)';
    PRINT '  - Archived Events: 0';
    PRINT '  - Next EventId will be: 1';
    PRINT '  - Next AttendanceId will be: 1';
    PRINT '';
    PRINT 'Transaction Status: Ready to COMMIT';
    PRINT '';

    -- COMMIT the transaction
    COMMIT TRANSACTION;
    PRINT '✓ Transaction COMMITTED - All changes saved!';

END TRY
BEGIN CATCH
    -- Rollback on error
    ROLLBACK TRANSACTION;
    
    PRINT '';
    PRINT '========================================';
    PRINT '❌ ERROR OCCURRED - ROLLBACK EXECUTED';
    PRINT '========================================';
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR(10));
    PRINT '';
    PRINT 'No changes were made to the database.';
    PRINT '';
    
END CATCH;

GO

-- =============================================
-- VERIFICATION QUERY
-- =============================================
PRINT '========================================';
PRINT 'VERIFICATION:';
PRINT '========================================';

SELECT 
    'Event' AS TableName, 
    COUNT(*) AS RecordCount,
    IDENT_CURRENT('Event') AS CurrentIdentity
FROM Event

UNION ALL

SELECT 
    'Attendance' AS TableName, 
    COUNT(*) AS RecordCount,
    NULL AS CurrentIdentity
FROM Attendance

UNION ALL

SELECT 
    'ArchivedEvents' AS TableName, 
    COUNT(*) AS RecordCount,
    NULL AS CurrentIdentity
FROM ArchivedEvents

UNION ALL

SELECT 
    'Fines (with AttendanceId)' AS TableName, 
    COUNT(*) AS RecordCount,
    NULL AS CurrentIdentity
FROM Fines
WHERE AttendanceId IS NOT NULL;

PRINT '';
PRINT '========================================';
PRINT 'Script Execution Complete!';
PRINT '========================================';
