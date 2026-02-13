-- ============================================================
-- SIMPLE RESET: Fees and Fines (Back to Start)
-- ============================================================
-- WARNING: This will permanently delete ALL payment data!
-- Use with caution - NO UNDO!
-- ============================================================

USE PortaliBits;
GO

BEGIN TRANSACTION;

    -- Delete all related records first (foreign key constraints)
    DELETE FROM PaymentTransactions;
    DELETE FROM FinePaymentTransactions;
    DELETE FROM RemittanceItems;
    DELETE FROM Remittances;
    
    -- Delete notifications related to payments
    DELETE FROM Notifications WHERE NotificationType IN ('Payment', 'Remittance', 'Fine');
    
    -- Delete all Fees and Fines
    DELETE FROM Fees;
    DELETE FROM Fines;
    
    -- Reset identity seeds to 1
    DBCC CHECKIDENT ('Fees', RESEED, 0);
    DBCC CHECKIDENT ('Fines', RESEED, 0);
    DBCC CHECKIDENT ('PaymentTransactions', RESEED, 0);
    DBCC CHECKIDENT ('FinePaymentTransactions', RESEED, 0);
    DBCC CHECKIDENT ('Remittances', RESEED, 0);
    DBCC CHECKIDENT ('RemittanceItems', RESEED, 0);

COMMIT TRANSACTION;

-- Verify
SELECT 'Fees' AS Table, COUNT(*) AS Count FROM Fees
UNION ALL
SELECT 'Fines', COUNT(*) FROM Fines
UNION ALL
SELECT 'PaymentTransactions', COUNT(*) FROM PaymentTransactions
UNION ALL
SELECT 'FinePaymentTransactions', COUNT(*) FROM FinePaymentTransactions
UNION ALL
SELECT 'Remittances', COUNT(*) FROM Remittances
UNION ALL
SELECT 'RemittanceItems', COUNT(*) FROM RemittanceItems;

PRINT '✅ Reset Complete! Next IDs will start from 1';
GO
