-- ============================================================
-- RESET ALL FINES (Manual and Event-Based)
-- ============================================================
-- Description: Safely deletes all fine data and resets IDs.
-- ============================================================

BEGIN TRANSACTION;
BEGIN TRY
    PRINT 'Starting Fines Reset...';

    -- 1. Unlink Fines from RemittanceItems
    UPDATE [dbo].[RemittanceItems] SET [FineId] = NULL;

    -- 2. Delete Fine Payment Transactions
    DELETE FROM [dbo].[FinePaymentTransactions];

    -- 3. Delete All Fines
    DELETE FROM [dbo].[Fines];

    -- 4. Reset Identity Counters
    DBCC CHECKIDENT ('[dbo].[Fines]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[FinePaymentTransactions]', RESEED, 0);

    COMMIT TRANSACTION;

    -- FINAL VERIFICATION TABLE
    SELECT 'Fines' as [Module], 'Fines' as [Table], COUNT(*) as [Final Count] FROM [dbo].[Fines]
    UNION ALL SELECT 'Fines', 'Fine Transactions', COUNT(*) FROM [dbo].[FinePaymentTransactions];

    PRINT 'SUCCESS: Fines Reset Complete. All counts should be 0.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
END CATCH
GO
