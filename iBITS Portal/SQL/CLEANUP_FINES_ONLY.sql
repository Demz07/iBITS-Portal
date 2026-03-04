-- ============================================================
-- 2. DELETE ALL FINES (WITH IDENTITY RESET)
-- ============================================================
BEGIN TRANSACTION;
BEGIN TRY
    -- Unlink from Remittances first
    UPDATE [dbo].[RemittanceItems] SET [FineId] = NULL;
    
    -- Delete Transactions and Fines
    DELETE FROM [dbo].[FinePaymentTransactions];
    DELETE FROM [dbo].[Fines];

    -- Reset Identity Counters to start from 1
    DBCC CHECKIDENT ('[dbo].[Fines]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[FinePaymentTransactions]', RESEED, 0);

    COMMIT TRANSACTION;
    SELECT 'Fines Cleanup' as [Operation], 'Success' as [Status], 'IDs Reset to 1' as [Note];
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    SELECT 'Fines Cleanup' as [Operation], 'Failed' as [Status], ERROR_MESSAGE() as [Message];
END CATCH
