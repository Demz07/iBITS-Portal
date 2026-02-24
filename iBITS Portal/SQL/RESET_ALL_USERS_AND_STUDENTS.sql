-- ============================================================
-- RESET ALL STUDENTS AND IDENTITY USERS (EXCEPT ADMIN)
-- ============================================================
-- Description: Wipes all student records and user accounts
--              while strictly preserving the 'admin' account.
-- ============================================================

BEGIN TRANSACTION;
BEGIN TRY
    PRINT '!!! STARTING TOTAL USER AND STUDENT RESET !!!';

    -- 1. Identify Admin ID to prevent accidental deletion
    DECLARE @AdminId NVARCHAR(450);
    SELECT @AdminId = Id FROM [dbo].[AspNetUsers] WHERE UserName = 'admin' OR Email = 'admin@ibits.edu.ph';

    IF @AdminId IS NULL
    BEGIN
        PRINT 'WARNING: Admin account not found. Proceeding with caution...';
    END

    -- 2. Cleanup Student Dependencies (Financials, Attendance, etc.)
    -- This ensures no FK violations occur.
    DELETE FROM [dbo].[RemittanceItems];
    DELETE FROM [dbo].[FinePaymentTransactions];
    DELETE FROM [dbo].[Fines];
    DELETE FROM [dbo].[PaymentTransactions];
    DELETE FROM [dbo].[Fees];
    DELETE FROM [dbo].[Attendance];
    DELETE FROM [dbo].[Notification];
    DELETE FROM [dbo].[UserAnnouncementDismissals];
    DELETE FROM [dbo].[PendingRoleChanges];
    PRINT '-> Cleaned up all student dependencies (Fines, Fees, Attendance, Notifications).';

    -- 3. Cleanup Identity Tables (Except Admin)
    DELETE FROM [dbo].[AspNetUserRoles] WHERE UserId != @AdminId OR @AdminId IS NULL;
    DELETE FROM [dbo].[AspNetUserClaims] WHERE UserId != @AdminId OR @AdminId IS NULL;
    DELETE FROM [dbo].[AspNetUserLogins] WHERE UserId != @AdminId OR @AdminId IS NULL;
    DELETE FROM [dbo].[AspNetUserTokens] WHERE UserId != @AdminId OR @AdminId IS NULL;
    PRINT '-> Cleaned up user roles, claims, and tokens.';

    -- 4. Delete Student Records
    -- We keep 'admin' if it exists in the Student table for some reason
    DELETE FROM [dbo].[Student] WHERE [StudentNum] != 'admin';
    PRINT '-> Deleted all Student records.';

    -- 5. Delete Identity Users (Except Admin)
    DELETE FROM [dbo].[AspNetUsers] WHERE Id != @AdminId OR @AdminId IS NULL;
    PRINT '-> Deleted all user accounts except Admin.';

    -- 6. Reset System State
    DELETE FROM [dbo].[Remittances];
    DELETE FROM [dbo].[ActivityLogs]; -- Optional: clearing logs for a fresh start
    PRINT '-> Reset Remittances and Activity Logs.';

    -- 7. Reset Identity Counters for related tables
    DBCC CHECKIDENT ('[dbo].[RemittanceItems]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[Remittances]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[Attendance]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[Fees]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[Fines]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[ActivityLogs]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[Notification]', RESEED, 0);
    PRINT '-> Reset all identity counters to 1.';

    COMMIT TRANSACTION;

    -- FINAL VERIFICATION TABLE
    SELECT 'Accounts' as [Module], 'Identity Users' as [Table], COUNT(*) as [Count] FROM [dbo].[AspNetUsers]
    UNION ALL SELECT 'Accounts', 'Student Records', COUNT(*) FROM [dbo].[Student]
    UNION ALL SELECT 'Accounts', 'User Roles', COUNT(*) FROM [dbo].[AspNetUserRoles]
    UNION ALL SELECT 'Financials', 'Active Fees', COUNT(*) FROM [dbo].[Fees]
    UNION ALL SELECT 'Financials', 'Active Fines', COUNT(*) FROM [dbo].[Fines]
    UNION ALL SELECT 'System', 'Remittances', COUNT(*) FROM [dbo].[Remittances];

    PRINT '==============================================';
    PRINT 'SUCCESS: TOTAL SYSTEM RESET COMPLETE.';
    PRINT 'Preserved: Admin Account.';
    PRINT '==============================================';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'CRITICAL ERROR: ' + ERROR_MESSAGE();
END CATCH
GO
