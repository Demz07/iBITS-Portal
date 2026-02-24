-- ============================================================
-- RESET ALL PORTAL DATA (EVENTS, FEES, AND FINES)
-- ============================================================
-- Description: Full cleanup of events and financial records.
-- ============================================================

BEGIN TRANSACTION;
BEGIN TRY
    PRINT '!!! STARTING FULL SYSTEM DATA RESET !!!';

    -- 1. Unlink All from RemittanceItems
    UPDATE [dbo].[RemittanceItems] SET [FeeId] = NULL, [FineId] = NULL;
    PRINT '-> Unlinked all records from Remittances.';

    -- 2. Cleanup Fines Section
    DELETE FROM [dbo].[FinePaymentTransactions];
    DELETE FROM [dbo].[Fines];
    PRINT '-> Deleted all Fines and Fine Transactions.';

    -- 3. Cleanup Fees Section
    DELETE FROM [dbo].[PaymentTransactions];
    DELETE FROM [dbo].[Fees];
    PRINT '-> Deleted all Fees and Fee Transactions.';

    -- 4. Cleanup Events Section
    DELETE FROM [dbo].[Attendance];
    DELETE FROM [dbo].[Event];
    PRINT '-> Deleted all Attendance and Events.';

    -- 5. Reset All Identity Counters
    DBCC CHECKIDENT ('[dbo].[Event]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[Attendance]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[Fines]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[Fees]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[PaymentTransactions]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[FinePaymentTransactions]', RESEED, 0);
    PRINT '-> Reset ALL identity counters to 1.';

    COMMIT TRANSACTION;
    PRINT '==============================================';
    PRINT 'SUCCESS: FULL SYSTEM RESET COMPLETE.';
    PRINT '==============================================';
    
    -- Summary Check
    SELECT 'Events' as [Table], COUNT(*) as [Count] FROM [dbo].[Event]
    UNION ALL SELECT 'Attendance', COUNT(*) FROM [dbo].[Attendance]
    UNION ALL SELECT 'Fees', COUNT(*) FROM [dbo].[Fees]
    UNION ALL SELECT 'Fines', COUNT(*) FROM [dbo].[Fines]
    UNION ALL SELECT 'Remittances (Preserved)', COUNT(*) FROM [dbo].[Remittances];

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'CRITICAL ERROR: ' + ERROR_MESSAGE();
END CATCH
GO
