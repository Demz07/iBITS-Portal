-- ============================================================
-- MASTER SYSTEM CLEANUP SCRIPT (CONSOLIDATED)
-- ============================================================
-- Description: Safely deletes Users (except Admin), Students, 
-- Fines, Fees, and Events with a summary feedback table.
-- ALL Identity columns are reset to start from 1.
-- ============================================================

SET NOCOUNT ON;

DECLARE @Results TABLE (
    [Step] NVARCHAR(100),
    [Status] NVARCHAR(20),
    [RowsDeleted] INT,
    [Notes] NVARCHAR(MAX)
);

BEGIN TRANSACTION;
BEGIN TRY
    -- 1. CLEANUP FINANCIALS & REMITTANCES
    DELETE FROM [dbo].[RemittanceItems];
    INSERT INTO @Results VALUES ('Remittance Items', 'Success', @@ROWCOUNT, 'Cleared all items');
    
    DELETE FROM [dbo].[Remittances];
    INSERT INTO @Results VALUES ('Remittances', 'Success', @@ROWCOUNT, 'Cleared all batches');

    DELETE FROM [dbo].[FinePaymentTransactions];
    DELETE FROM [dbo].[Fines];
    INSERT INTO @Results VALUES ('Fines', 'Success', @@ROWCOUNT, 'Cleared fines and transactions');

    DELETE FROM [dbo].[PaymentTransactions];
    DELETE FROM [dbo].[Fees];
    INSERT INTO @Results VALUES ('Fees', 'Success', @@ROWCOUNT, 'Cleared fees and transactions');

    -- 2. CLEANUP EVENTS & ATTENDANCE
    DELETE FROM [dbo].[Attendance];
    DELETE FROM [dbo].[Event];
    INSERT INTO @Results VALUES ('Events & Attendance', 'Success', @@ROWCOUNT, 'Cleared events data');

    -- 3. CLEANUP STUDENT LOGS & NOTIFICATIONS
    DELETE FROM [dbo].[Notification];
    DELETE FROM [dbo].[ActivityLogs];
    DELETE FROM [dbo].[UserAnnouncementDismissals];
    INSERT INTO @Results VALUES ('Student Activity', 'Success', @@ROWCOUNT, 'Cleared logs/notifications');

    -- 4. DELETE STUDENTS (EXCEPT ADMIN)
    -- Using Student table name from context configuration
    DELETE FROM [dbo].[Student] 
    WHERE StudentNum <> 'admin' AND StudentEmail <> 'admin@ibits.edu.ph';
    INSERT INTO @Results VALUES ('Students', 'Success', @@ROWCOUNT, 'Excluding admin');

    -- 5. DELETE ASP USERS (EXCEPT ADMIN)
    DECLARE @AdminId NVARCHAR(450) = (SELECT Id FROM AspNetUsers WHERE UserName = 'admin' OR Email = 'admin@ibits.edu.ph');
    
    DELETE FROM AspNetUserRoles WHERE UserId <> @AdminId OR UserId IS NULL;
    DELETE FROM AspNetUserClaims WHERE UserId <> @AdminId OR UserId IS NULL;
    DELETE FROM AspNetUserLogins WHERE UserId <> @AdminId OR UserId IS NULL;
    DELETE FROM AspNetUserTokens WHERE UserId <> @AdminId OR UserId IS NULL;
    DELETE FROM AspNetUsers WHERE Id <> @AdminId OR Id IS NULL;
    INSERT INTO @Results VALUES ('Identity Users', 'Success', @@ROWCOUNT, 'Admin account preserved');

    -- ============================================================
    -- RESET ALL IDENTITY COUNTERS TO 1
    -- ============================================================
    PRINT 'Resetting Identity Counters...';
    
    DBCC CHECKIDENT ('[dbo].[Event]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[Attendance]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[Fines]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[Fees]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[PaymentTransactions]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[FinePaymentTransactions]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[Remittances]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[RemittanceItems]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[Notification]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[ActivityLogs]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[UserAnnouncementDismissals]', RESEED, 0);

    INSERT INTO @Results VALUES ('Identity Reset', 'Success', NULL, 'All tables reset to start from 1');

    COMMIT TRANSACTION;
    INSERT INTO @Results VALUES ('FINAL STATUS', 'COMPLETE', NULL, 'System reset successful');

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    INSERT INTO @Results VALUES ('ERROR', 'FAILED', 0, ERROR_MESSAGE());
END CATCH

-- DISPLAY FEEDBACK TABLE
SELECT 
    [Step] AS [System Component],
    [Status],
    ISNULL(CAST([RowsDeleted] AS VARCHAR), '-') AS [Impact],
    [Notes]
FROM @Results;
