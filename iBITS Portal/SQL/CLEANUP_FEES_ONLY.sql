-- ============================================================
-- 3. DELETE ALL FEES (WITH IDENTITY RESET)
-- ============================================================
BEGIN TRANSACTION;
BEGIN TRY
    -- Unlink from Remittances first
    UPDATE [dbo].[RemittanceItems] SET [FeeId] = NULL;
    
    -- Delete Transactions and Fees
    DELETE FROM [dbo].[PaymentTransactions];
    DELETE FROM [dbo].[Fees];

    -- Reset Identity Counters to start from 1
    DBCC CHECKIDENT ('[dbo].[Fees]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[PaymentTransactions]', RESEED, 0);

    COMMIT TRANSACTION;
    SELECT 'Fees Cleanup' as [Operation], 'Success' as [Status], 'IDs Reset to 1' as [Note];
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    SELECT 'Fees Cleanup' as [Operation], 'Failed' as [Status], ERROR_MESSAGE() as [Message];
END CATCH
