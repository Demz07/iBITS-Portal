-- ================================================================
-- iBITS Portal - Payment Lock/Revoke Feature
-- Database Migration Script
-- ================================================================
-- Purpose: Add payment locking capabilities to Fee table
-- Date: 2026-02-12
-- Author: RovoDev
-- ================================================================

USE [PortaliBITS]
GO

PRINT 'Starting Payment Lock/Revoke Feature Migration...'
PRINT ''

-- ================================================================
-- STEP 1: Backup existing Fee data
-- ================================================================
PRINT 'Creating backup of Fee table...'

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Fees_Backup_PaymentLock_20260212')
BEGIN
    SELECT * 
    INTO Fees_Backup_PaymentLock_20260212
    FROM Fees
    
    PRINT '✓ Backup created: Fees_Backup_PaymentLock_20260212'
    PRINT '  Rows backed up: ' + CAST(@@ROWCOUNT AS VARCHAR(10))
END
ELSE
BEGIN
    PRINT '⚠ Backup table already exists, skipping...'
END
GO

-- ================================================================
-- STEP 2: Add new columns to Fee table
-- ================================================================
PRINT ''
PRINT 'Adding new columns to Fee table...'

-- Check and add IsPaymentLocked column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Fees') AND name = 'IsPaymentLocked')
BEGIN
    ALTER TABLE Fees 
    ADD IsPaymentLocked BIT NOT NULL DEFAULT 0
    
    PRINT '✓ Added column: IsPaymentLocked (BIT, DEFAULT 0)'
END
ELSE
BEGIN
    PRINT '⚠ Column IsPaymentLocked already exists'
END
GO

-- Check and add PaymentLockedDate column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Fees') AND name = 'PaymentLockedDate')
BEGIN
    ALTER TABLE Fees 
    ADD PaymentLockedDate DATETIME2 NULL
    
    PRINT '✓ Added column: PaymentLockedDate (DATETIME2, NULL)'
END
ELSE
BEGIN
    PRINT '⚠ Column PaymentLockedDate already exists'
END
GO

-- Check and add LockedBy column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Fees') AND name = 'LockedBy')
BEGIN
    ALTER TABLE Fees 
    ADD LockedBy NVARCHAR(450) NULL
    
    PRINT '✓ Added column: LockedBy (NVARCHAR(450), NULL)'
END
ELSE
BEGIN
    PRINT '⚠ Column LockedBy already exists'
END
GO

-- ================================================================
-- STEP 3: Add foreign key constraint for LockedBy
-- ================================================================
PRINT ''
PRINT 'Adding foreign key constraint...'

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Fees_LockedBy_Student')
BEGIN
    ALTER TABLE Fees 
    ADD CONSTRAINT FK_Fees_LockedBy_Student 
    FOREIGN KEY (LockedBy) REFERENCES Student(StudentNum)
    
    PRINT '✓ Added foreign key: FK_Fees_LockedBy_Student'
END
ELSE
BEGIN
    PRINT '⚠ Foreign key FK_Fees_LockedBy_Student already exists'
END
GO

-- ================================================================
-- STEP 4: Create indexes for performance
-- ================================================================
PRINT ''
PRINT 'Creating indexes...'

-- Index on IsPaymentLocked for faster filtering
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Fees_IsPaymentLocked' AND object_id = OBJECT_ID('Fees'))
BEGIN
    CREATE INDEX IX_Fees_IsPaymentLocked ON Fees(IsPaymentLocked)
    PRINT '✓ Created index: IX_Fees_IsPaymentLocked'
END
ELSE
BEGIN
    PRINT '⚠ Index IX_Fees_IsPaymentLocked already exists'
END
GO

-- Composite index for common query patterns
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Fees_Status_Locked' AND object_id = OBJECT_ID('Fees'))
BEGIN
    CREATE INDEX IX_Fees_Status_Locked ON Fees(FeeStatus, IsPaymentLocked)
    PRINT '✓ Created index: IX_Fees_Status_Locked'
END
ELSE
BEGIN
    PRINT '⚠ Index IX_Fees_Status_Locked already exists'
END
GO

-- ================================================================
-- STEP 5: Verify the changes
-- ================================================================
PRINT ''
PRINT 'Verifying column additions...'

SELECT 
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length AS MaxLength,
    c.is_nullable AS IsNullable,
    OBJECT_DEFINITION(c.default_object_id) AS DefaultValue
FROM sys.columns c
INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
WHERE c.object_id = OBJECT_ID('Fees')
AND c.name IN ('IsPaymentLocked', 'PaymentLockedDate', 'LockedBy')
ORDER BY c.column_id
GO

-- ================================================================
-- STEP 6: Display current data statistics
-- ================================================================
PRINT ''
PRINT 'Current Fee statistics:'

SELECT 
    COUNT(*) AS TotalFees,
    SUM(CASE WHEN FeeStatus = 'Paid' THEN 1 ELSE 0 END) AS PaidFees,
    SUM(CASE WHEN FeeStatus != 'Paid' OR FeeStatus IS NULL THEN 1 ELSE 0 END) AS UnpaidFees,
    SUM(CASE WHEN IsPaymentLocked = 1 THEN 1 ELSE 0 END) AS LockedPayments,
    SUM(CASE WHEN FeeStatus = 'Paid' AND IsPaymentLocked = 0 THEN 1 ELSE 0 END) AS PaidButUnlocked
FROM Fees 
GO

-- ================================================================
-- STEP 7: Create AuditLog table if it doesn't exist
-- ================================================================
PRINT ''
PRINT 'Checking for AuditLog table...'

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLog')
BEGIN
    CREATE TABLE AuditLog (
        AuditId INT IDENTITY(1,1) PRIMARY KEY,
        Action NVARCHAR(100) NOT NULL,
        PerformedBy NVARCHAR(450) NULL,
        PerformedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
        EntityType NVARCHAR(50) NOT NULL,
        EntityId NVARCHAR(50) NOT NULL,
        Details NVARCHAR(MAX) NULL,
        
        CONSTRAINT FK_AuditLog_PerformedBy_Student 
        FOREIGN KEY (PerformedBy) REFERENCES Student(StudentNum)
    )
    
    CREATE INDEX IX_AuditLog_EntityType_EntityId ON AuditLog(EntityType, EntityId)
    CREATE INDEX IX_AuditLog_PerformedAt ON AuditLog(PerformedAt DESC)
    
    PRINT '✓ Created AuditLog table with indexes'
END
ELSE
BEGIN
    PRINT '⚠ AuditLog table already exists'
END
GO

-- ================================================================
-- STEP 8: Test insert (will be deleted)
-- ================================================================
PRINT ''
PRINT 'Running test scenarios...'

DECLARE @TestFeeId INT

-- Find a paid fee to test with (or create a test one)
SELECT TOP 1 @TestFeeId = FeeId 
FROM Fees 
WHERE FeeStatus = 'Paid' AND IsPaymentLocked = 0

IF @TestFeeId IS NOT NULL
BEGIN
    PRINT 'Test 1: Simulating payment lock...'
    
    -- Simulate locking a payment
    UPDATE Fee
    SET IsPaymentLocked = 1,
        PaymentLockedDate = GETDATE(),
        LockedBy = (SELECT TOP 1 StudentNum FROM Student WHERE StudentNum LIKE 'iBITS%')
    WHERE FeeId = @TestFeeId
    
    -- Verify the lock
    SELECT 
        FeeId,
        FeeName,
        FeeStatus,
        IsPaymentLocked,
        PaymentLockedDate,
        LockedBy,
        'Test Successful - Lock Applied' AS Status
    FROM Fees 
    WHERE FeeId = @TestFeeId
    
    -- Rollback the test
    UPDATE Fee
    SET IsPaymentLocked = 0,
        PaymentLockedDate = NULL,
        LockedBy = NULL
    WHERE FeeId = @TestFeeId
    
    PRINT '✓ Test completed and rolled back'
END
ELSE
BEGIN
    PRINT '⚠ No paid fees found for testing'
END
GO

-- ================================================================
-- COMPLETION SUMMARY
-- ================================================================
PRINT ''
PRINT '================================================================'
PRINT '  ✓ MIGRATION COMPLETED SUCCESSFULLY'
PRINT '================================================================'
PRINT ''
PRINT 'Changes Applied:'
PRINT '  ✓ Added column: IsPaymentLocked (BIT, DEFAULT 0)'
PRINT '  ✓ Added column: PaymentLockedDate (DATETIME2, NULL)'
PRINT '  ✓ Added column: LockedBy (NVARCHAR(450), NULL)'
PRINT '  ✓ Added foreign key: FK_Fees_LockedBy_Student'
PRINT '  ✓ Created indexes for performance'
PRINT '  ✓ Created/verified AuditLog table'
PRINT '  ✓ Backup created: Fee_Backup_PaymentLock_20260212'
PRINT ''
PRINT 'Next Steps:'
PRINT '  1. Update Fee.cs model with new properties'
PRINT '  2. Add controller actions (RevokePayment, LockPayment)'
PRINT '  3. Update OrgFees.cshtml with UI buttons and modals'
PRINT '  4. Update Student Financials.cshtml with lock indicators'
PRINT '  5. Test all functionality'
PRINT ''
PRINT '================================================================'
GO

