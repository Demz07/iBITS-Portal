-- ============================================================
-- RESET FEES AND FEE PAYMENTS
-- ============================================================
-- Description: Safely deletes all fee data and resets IDs.
-- ============================================================

BEGIN TRANSACTION;
BEGIN TRY
    PRINT 'Starting Fees Reset...';

    -- 1. Unlink Fees from RemittanceItems
    UPDATE [dbo].[RemittanceItems] SET [FeeId] = NULL;

    -- 2. Delete Payment Transactions
    DELETE FROM [dbo].[PaymentTransactions];

    -- 3. Delete Fees
    DELETE FROM [dbo].[Fees];

    -- 4. Reset Identity Counters
    DBCC CHECKIDENT ('[dbo].[Fees]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[PaymentTransactions]', RESEED, 0);

    COMMIT TRANSACTION;

    -- FINAL VERIFICATION TABLE
    SELECT 'Fees' as [Module], 'Fees' as [Table], COUNT(*) as [Final Count] FROM [dbo].[Fees]
    UNION ALL SELECT 'Fees', 'Fee Transactions', COUNT(*) FROM [dbo].[PaymentTransactions];

    PRINT 'SUCCESS: Fees Reset Complete. All counts should be 0.';
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    PRINT 'ERROR: ' + ERROR_MESSAGE();
END CATCH
GO
