-- ============================================================
-- SQL SCRIPT: Remittance System Schema
-- iBITS Portal - Class Treasurer & Org Treasurer Workflow
-- ============================================================
-- Run this script on your PortaliBits database
-- Make sure to backup your database before running!
-- ============================================================

USE [PortaliBits];
GO

-- ============================================================
-- STEP 1: Create Remittances Table
-- Tracks batch remittances from Class Treasurer to Org Treasurer
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Remittances')
BEGIN
    CREATE TABLE [dbo].[Remittances] (
        [RemittanceId]      INT IDENTITY(1,1) NOT NULL,
        [BatchCode]         NVARCHAR(50) NOT NULL,           -- e.g., "RMT-2026-0001"
        [FeeName]           NVARCHAR(200) NULL,              -- Fee category name (for fee remittances)
        [FineCategory]      NVARCHAR(200) NULL,              -- Fine category (for fine remittances)
        [RemittanceType]    NVARCHAR(20) NOT NULL,           -- "Fee" or "Fine"
        [Section]           NVARCHAR(100) NOT NULL,          -- YearLevelSection of the class
        [TotalAmount]       DECIMAL(18,2) NOT NULL,          -- Sum of all payments in this remittance
        [TotalStudents]     INT NOT NULL,                    -- Count of students paid
        [SubmittedBy]       NVARCHAR(450) NOT NULL,          -- Class Treasurer StudentNum
        [SubmittedDate]     DATETIME2 NOT NULL DEFAULT GETDATE(),
        [Status]            NVARCHAR(20) NOT NULL DEFAULT 'Pending', -- Pending | Validated | Rejected
        [ValidatedBy]       NVARCHAR(450) NULL,              -- Org Treasurer StudentNum
        [ValidationDate]    DATETIME2 NULL,                  -- THE OFFICIAL DATE (set when validated)
        [ValidationNotes]   NVARCHAR(1000) NULL,             -- Notes from Org Treasurer
        [RejectionReason]   NVARCHAR(1000) NULL,             -- Reason if rejected
        [AcademicYear]      NVARCHAR(20) NULL,               -- e.g., "2025-2026"
        [CreatedAt]         DATETIME2 NOT NULL DEFAULT GETDATE(),
        [UpdatedAt]         DATETIME2 NULL,
        
        CONSTRAINT [PK_Remittances] PRIMARY KEY CLUSTERED ([RemittanceId]),
        CONSTRAINT [UQ_Remittances_BatchCode] UNIQUE ([BatchCode]),
        CONSTRAINT [CK_Remittances_Status] CHECK ([Status] IN ('Pending', 'Validated', 'Rejected')),
        CONSTRAINT [CK_Remittances_Type] CHECK ([RemittanceType] IN ('Fee', 'Fine'))
    );
    
    PRINT 'Created table: Remittances';
END
ELSE
BEGIN
    PRINT 'Table Remittances already exists - skipping creation';
END
GO

-- ============================================================
-- STEP 2: Create RemittanceItems Table
-- Individual payment records within a remittance batch
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RemittanceItems')
BEGIN
    CREATE TABLE [dbo].[RemittanceItems] (
        [RemittanceItemId]  INT IDENTITY(1,1) NOT NULL,
        [RemittanceId]      INT NOT NULL,                    -- FK to Remittances
        [FeeId]             INT NULL,                        -- FK to Fees (if fee payment)
        [FineId]            INT NULL,                        -- FK to Fines (if fine payment)
        [StudentNum]        NVARCHAR(450) NOT NULL,          -- Student who paid
        [StudentName]       NVARCHAR(300) NULL,              -- Cached student name for reporting
        [Amount]            DECIMAL(18,2) NOT NULL,          -- Amount paid
        [CollectionDate]    DATETIME2 NOT NULL,              -- When Class Treasurer marked as paid
        [PaymentMethod]     NVARCHAR(50) NULL,               -- Cash, GCash, etc.
        [TransactionRef]    NVARCHAR(100) NULL,              -- Reference number if any
        [Notes]             NVARCHAR(500) NULL,
        
        CONSTRAINT [PK_RemittanceItems] PRIMARY KEY CLUSTERED ([RemittanceItemId]),
        CONSTRAINT [FK_RemittanceItems_Remittances] FOREIGN KEY ([RemittanceId]) 
            REFERENCES [dbo].[Remittances]([RemittanceId]) ON DELETE CASCADE,
        CONSTRAINT [FK_RemittanceItems_Fees] FOREIGN KEY ([FeeId]) 
            REFERENCES [dbo].[Fees]([FeeId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RemittanceItems_Fines] FOREIGN KEY ([FineId]) 
            REFERENCES [dbo].[Fines]([FineId]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RemittanceItems_Students] FOREIGN KEY ([StudentNum]) 
            REFERENCES [dbo].[Student]([StudentNum]) ON DELETE NO ACTION
    );
    
    PRINT 'Created table: RemittanceItems';
END
ELSE
BEGIN
    PRINT 'Table RemittanceItems already exists - skipping creation';
END
GO

-- ============================================================
-- STEP 3: Add Remittance-related columns to Fees table
-- ============================================================
-- Add RemittanceStatus column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Fees') AND name = 'RemittanceStatus')
BEGIN
    ALTER TABLE [dbo].[Fees] 
    ADD [RemittanceStatus] NVARCHAR(20) NOT NULL DEFAULT 'NotRemitted';
    
    PRINT 'Added column: Fees.RemittanceStatus';
END
GO

-- Add RemittanceId column (FK)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Fees') AND name = 'RemittanceId')
BEGIN
    ALTER TABLE [dbo].[Fees] 
    ADD [RemittanceId] INT NULL;
    
    PRINT 'Added column: Fees.RemittanceId';
END
GO

-- Add CollectedBy column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Fees') AND name = 'CollectedBy')
BEGIN
    ALTER TABLE [dbo].[Fees] 
    ADD [CollectedBy] NVARCHAR(450) NULL;
    
    PRINT 'Added column: Fees.CollectedBy';
END
GO

-- Add CollectionDate column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Fees') AND name = 'CollectionDate')
BEGIN
    ALTER TABLE [dbo].[Fees] 
    ADD [CollectionDate] DATETIME2 NULL;
    
    PRINT 'Added column: Fees.CollectionDate';
END
GO

-- Add OfficialPaymentDate column (set when Org Treasurer validates)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Fees') AND name = 'OfficialPaymentDate')
BEGIN
    ALTER TABLE [dbo].[Fees] 
    ADD [OfficialPaymentDate] DATETIME2 NULL;
    
    PRINT 'Added column: Fees.OfficialPaymentDate';
END
GO

-- Add FK constraint for RemittanceId
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Fees_Remittances')
BEGIN
    ALTER TABLE [dbo].[Fees]
    ADD CONSTRAINT [FK_Fees_Remittances] FOREIGN KEY ([RemittanceId])
        REFERENCES [dbo].[Remittances]([RemittanceId]) ON DELETE SET NULL;
    
    PRINT 'Added FK: Fees.RemittanceId -> Remittances';
END
GO

-- Add check constraint for RemittanceStatus
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_Fees_RemittanceStatus')
BEGIN
    ALTER TABLE [dbo].[Fees]
    ADD CONSTRAINT [CK_Fees_RemittanceStatus] 
        CHECK ([RemittanceStatus] IN ('NotRemitted', 'PendingRemittance', 'Remitted'));
    
    PRINT 'Added constraint: CK_Fees_RemittanceStatus';
END
GO

-- ============================================================
-- STEP 4: Add Remittance-related columns to Fines table
-- ============================================================
-- Add RemittanceStatus column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Fines') AND name = 'RemittanceStatus')
BEGIN
    ALTER TABLE [dbo].[Fines] 
    ADD [RemittanceStatus] NVARCHAR(20) NOT NULL DEFAULT 'NotRemitted';
    
    PRINT 'Added column: Fines.RemittanceStatus';
END
GO

-- Add RemittanceId column (FK)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Fines') AND name = 'RemittanceId')
BEGIN
    ALTER TABLE [dbo].[Fines] 
    ADD [RemittanceId] INT NULL;
    
    PRINT 'Added column: Fines.RemittanceId';
END
GO

-- Add CollectedBy column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Fines') AND name = 'CollectedBy')
BEGIN
    ALTER TABLE [dbo].[Fines] 
    ADD [CollectedBy] NVARCHAR(450) NULL;
    
    PRINT 'Added column: Fines.CollectedBy';
END
GO

-- Add CollectionDate column
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Fines') AND name = 'CollectionDate')
BEGIN
    ALTER TABLE [dbo].[Fines] 
    ADD [CollectionDate] DATETIME2 NULL;
    
    PRINT 'Added column: Fines.CollectionDate';
END
GO

-- Add OfficialPaymentDate column (set when Org Treasurer validates)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('dbo.Fines') AND name = 'OfficialPaymentDate')
BEGIN
    ALTER TABLE [dbo].[Fines] 
    ADD [OfficialPaymentDate] DATETIME2 NULL;
    
    PRINT 'Added column: Fines.OfficialPaymentDate';
END
GO

-- Add FK constraint for RemittanceId
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Fines_Remittances')
BEGIN
    ALTER TABLE [dbo].[Fines]
    ADD CONSTRAINT [FK_Fines_Remittances] FOREIGN KEY ([RemittanceId])
        REFERENCES [dbo].[Remittances]([RemittanceId]) ON DELETE SET NULL;
    
    PRINT 'Added FK: Fines.RemittanceId -> Remittances';
END
GO

-- Add check constraint for RemittanceStatus
IF NOT EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_Fines_RemittanceStatus')
BEGIN
    ALTER TABLE [dbo].[Fines]
    ADD CONSTRAINT [CK_Fines_RemittanceStatus] 
        CHECK ([RemittanceStatus] IN ('NotRemitted', 'PendingRemittance', 'Remitted'));
    
    PRINT 'Added constraint: CK_Fines_RemittanceStatus';
END
GO

-- ============================================================
-- STEP 5: Create Indexes for Performance
-- ============================================================
-- Index on Remittances.Status for filtering pending remittances
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Remittances_Status')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Remittances_Status] 
    ON [dbo].[Remittances]([Status]) 
    INCLUDE ([Section], [SubmittedBy], [TotalAmount]);
    
    PRINT 'Created index: IX_Remittances_Status';
END
GO

-- Index on Remittances.Section for Class Treasurer queries
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Remittances_Section')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Remittances_Section] 
    ON [dbo].[Remittances]([Section], [Status]);
    
    PRINT 'Created index: IX_Remittances_Section';
END
GO

-- Index on RemittanceItems.RemittanceId
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_RemittanceItems_RemittanceId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_RemittanceItems_RemittanceId] 
    ON [dbo].[RemittanceItems]([RemittanceId]);
    
    PRINT 'Created index: IX_RemittanceItems_RemittanceId';
END
GO

-- Index on Fees.RemittanceStatus
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Fees_RemittanceStatus')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Fees_RemittanceStatus] 
    ON [dbo].[Fees]([RemittanceStatus], [FeeStatus]);
    
    PRINT 'Created index: IX_Fees_RemittanceStatus';
END
GO

-- Index on Fines.RemittanceStatus
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Fines_RemittanceStatus')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Fines_RemittanceStatus] 
    ON [dbo].[Fines]([RemittanceStatus], [FinesStatus]);
    
    PRINT 'Created index: IX_Fines_RemittanceStatus';
END
GO

-- ============================================================
-- STEP 6: Create Stored Procedure for Batch Code Generation
-- ============================================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sp_GenerateRemittanceBatchCode')
BEGIN
    DROP PROCEDURE [dbo].[sp_GenerateRemittanceBatchCode];
END
GO

CREATE PROCEDURE [dbo].[sp_GenerateRemittanceBatchCode]
    @BatchCode NVARCHAR(50) OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @Year NVARCHAR(4) = CAST(YEAR(GETDATE()) AS NVARCHAR(4));
    DECLARE @NextNum INT;
    
    -- Get the next sequence number for this year
    SELECT @NextNum = ISNULL(MAX(
        TRY_CAST(RIGHT(BatchCode, 4) AS INT)
    ), 0) + 1
    FROM [dbo].[Remittances]
    WHERE BatchCode LIKE 'RMT-' + @Year + '-%';
    
    -- Format: RMT-YYYY-NNNN
    SET @BatchCode = 'RMT-' + @Year + '-' + RIGHT('0000' + CAST(@NextNum AS NVARCHAR(4)), 4);
END
GO

PRINT 'Created stored procedure: sp_GenerateRemittanceBatchCode';
GO

-- ============================================================
-- STEP 7: Create View for Remittance Summary
-- ============================================================
IF EXISTS (SELECT * FROM sys.views WHERE name = 'vw_RemittanceSummary')
BEGIN
    DROP VIEW [dbo].[vw_RemittanceSummary];
END
GO

CREATE VIEW [dbo].[vw_RemittanceSummary]
AS
SELECT 
    r.RemittanceId,
    r.BatchCode,
    r.RemittanceType,
    COALESCE(r.FeeName, r.FineCategory) AS Category,
    r.Section,
    r.TotalAmount,
    r.TotalStudents,
    r.Status,
    r.SubmittedDate,
    r.ValidationDate,
    submitter.StudentFn + ' ' + submitter.StudentLn AS SubmittedByName,
    validator.StudentFn + ' ' + validator.StudentLn AS ValidatedByName,
    r.AcademicYear,
    DATEDIFF(DAY, r.SubmittedDate, ISNULL(r.ValidationDate, GETDATE())) AS DaysPending
FROM [dbo].[Remittances] r
LEFT JOIN [dbo].[Student] submitter ON r.SubmittedBy = submitter.StudentNum
LEFT JOIN [dbo].[Student] validator ON r.ValidatedBy = validator.StudentNum;
GO

PRINT 'Created view: vw_RemittanceSummary';
GO

-- ============================================================
-- VERIFICATION: Check all objects were created
-- ============================================================
PRINT '';
PRINT '============================================================';
PRINT 'VERIFICATION SUMMARY';
PRINT '============================================================';

SELECT 'Tables' AS ObjectType, name AS ObjectName 
FROM sys.tables 
WHERE name IN ('Remittances', 'RemittanceItems')
UNION ALL
SELECT 'Columns (Fees)', name 
FROM sys.columns 
WHERE object_id = OBJECT_ID('dbo.Fees') 
AND name IN ('RemittanceStatus', 'RemittanceId', 'CollectedBy', 'CollectionDate', 'OfficialPaymentDate')
UNION ALL
SELECT 'Columns (Fines)', name 
FROM sys.columns 
WHERE object_id = OBJECT_ID('dbo.Fines') 
AND name IN ('RemittanceStatus', 'RemittanceId', 'CollectedBy', 'CollectionDate', 'OfficialPaymentDate')
UNION ALL
SELECT 'Procedures', name 
FROM sys.procedures 
WHERE name = 'sp_GenerateRemittanceBatchCode'
UNION ALL
SELECT 'Views', name 
FROM sys.views 
WHERE name = 'vw_RemittanceSummary';

PRINT '';
PRINT 'Script completed successfully!';
PRINT 'Please verify the objects above were created correctly.';
GO
