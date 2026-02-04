/***************************************************************************
 * iBITS Portal - MASTER MIGRATION SCRIPT
 * Created: 2026-02-03
 * Version: 1.0
 * 
 * Purpose: Fix ALL pending migrations and database inconsistencies
 * 
 * This script will:
 *  1. Add missing UserAnnouncementDismissals table
 *  2. Add missing ExpiryDate column to Announcements table
 *  3. Add missing Remittance tables (Remittances, RemittanceItems)
 *  4. Add missing remittance tracking columns to Fees and Fines tables
 *  5. Add missing AmountPaid columns to Fees and Fines tables
 *  6. Ensure all indexes and foreign keys are properly created
 * 
 * IMPORTANT: 
 *  - Backup your database FIRST!
 *  - This script is IDEMPOTENT (safe to run multiple times)
 *  - It checks for existing objects before creating them
 * 
 * HOW TO RUN:
 *   1. Open SQL Server Management Studio
 *   2. Connect to your PortaliBITS database
 *   3. Run this script
 *   4. Review the output messages
 ***************************************************************************/

USE [PortaliBITS]
GO

SET NOCOUNT ON;
GO

PRINT '========================================================================';
PRINT 'iBITS Portal - MASTER MIGRATION SCRIPT';
PRINT 'Started at: ' + CONVERT(VARCHAR(50), GETDATE(), 120);
PRINT '========================================================================';
PRINT '';

BEGIN TRANSACTION;

DECLARE @ErrorOccurred BIT = 0;
DECLARE @ErrorMessage NVARCHAR(4000);

BEGIN TRY

    -- =====================================================================
    -- SECTION 1: ADD EXPIRYDATE COLUMN TO ANNOUNCEMENTS
    -- =====================================================================
    PRINT '========================================================================';
    PRINT 'SECTION 1: Announcements Table';
    PRINT '========================================================================';
    PRINT '';
    
    IF NOT EXISTS (SELECT * FROM sys.columns 
                   WHERE object_id = OBJECT_ID('dbo.Announcements') 
                   AND name = 'ExpiryDate')
    BEGIN
        PRINT '1.1: Adding ExpiryDate column to Announcements table...';
        ALTER TABLE [dbo].[Announcements]
        ADD [ExpiryDate] DATETIME2(7) NULL;
        PRINT '    ✓ ExpiryDate column added successfully';
    END
    ELSE
    BEGIN
        PRINT '1.1: ExpiryDate column already exists - SKIPPED';
    END
    PRINT '';

    -- =====================================================================
    -- SECTION 2: CREATE USERANNOUNCEMENTDISMISSALS TABLE
    -- =====================================================================
    PRINT '========================================================================';
    PRINT 'SECTION 2: UserAnnouncementDismissals Table';
    PRINT '========================================================================';
    PRINT '';
    
    IF NOT EXISTS (SELECT * FROM sys.objects 
                   WHERE object_id = OBJECT_ID(N'[dbo].[UserAnnouncementDismissals]') 
                   AND type in (N'U'))
    BEGIN
        PRINT '2.1: Creating UserAnnouncementDismissals table...';
        
        CREATE TABLE [dbo].[UserAnnouncementDismissals] (
            [Id] INT IDENTITY(1,1) NOT NULL,
            [StudentNum] NVARCHAR(450) NOT NULL,
            [AnnouncementId] INT NOT NULL,
            [DismissedAt] DATETIME2(7) NOT NULL DEFAULT (GETDATE()),
            
            CONSTRAINT [PK_UserAnnouncementDismissals] PRIMARY KEY CLUSTERED ([Id] ASC)
                WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, 
                      ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
        );
        
        PRINT '    ✓ Table created successfully';
        
        -- Create unique constraint
        PRINT '2.2: Creating unique constraint...';
        ALTER TABLE [dbo].[UserAnnouncementDismissals] 
        ADD CONSTRAINT [UQ_StudentAnnouncement] UNIQUE NONCLUSTERED 
        (
            [StudentNum] ASC,
            [AnnouncementId] ASC
        );
        PRINT '    ✓ Unique constraint created';
        
        -- Create foreign keys
        PRINT '2.3: Creating foreign keys...';
        ALTER TABLE [dbo].[UserAnnouncementDismissals]  
        WITH CHECK ADD CONSTRAINT [FK_UserAnnouncementDismissal_Student] 
        FOREIGN KEY([StudentNum])
        REFERENCES [dbo].[Student] ([StudentNum])
        ON DELETE CASCADE;
        
        ALTER TABLE [dbo].[UserAnnouncementDismissals] 
        CHECK CONSTRAINT [FK_UserAnnouncementDismissal_Student];
        
        ALTER TABLE [dbo].[UserAnnouncementDismissals]  
        WITH CHECK ADD CONSTRAINT [FK_UserAnnouncementDismissal_Announcement] 
        FOREIGN KEY([AnnouncementId])
        REFERENCES [dbo].[Announcements] ([Id])
        ON DELETE CASCADE;
        
        ALTER TABLE [dbo].[UserAnnouncementDismissals] 
        CHECK CONSTRAINT [FK_UserAnnouncementDismissal_Announcement];
        
        PRINT '    ✓ Foreign keys created';
        
        -- Create indexes
        PRINT '2.4: Creating indexes...';
        CREATE NONCLUSTERED INDEX [IX_UserAnnouncementDismissals_StudentNum]
        ON [dbo].[UserAnnouncementDismissals] ([StudentNum] ASC);
        
        CREATE NONCLUSTERED INDEX [IX_UserAnnouncementDismissals_AnnouncementId]
        ON [dbo].[UserAnnouncementDismissals] ([AnnouncementId] ASC);
        
        PRINT '    ✓ Indexes created';
    END
    ELSE
    BEGIN
        PRINT '2.1: UserAnnouncementDismissals table already exists - SKIPPED';
    END
    PRINT '';

    -- =====================================================================
    -- SECTION 3: CREATE REMITTANCES TABLE
    -- =====================================================================
    PRINT '========================================================================';
    PRINT 'SECTION 3: Remittances Table';
    PRINT '========================================================================';
    PRINT '';
    
    IF NOT EXISTS (SELECT * FROM sys.objects 
                   WHERE object_id = OBJECT_ID(N'[dbo].[Remittances]') 
                   AND type in (N'U'))
    BEGIN
        PRINT '3.1: Creating Remittances table...';
        
        CREATE TABLE [dbo].[Remittances] (
            [RemittanceId] INT IDENTITY(1,1) NOT NULL,
            [BatchCode] NVARCHAR(50) NOT NULL,
            [FeeName] NVARCHAR(200) NULL,
            [FineCategory] NVARCHAR(200) NULL,
            [RemittanceType] NVARCHAR(20) NOT NULL,
            [Section] NVARCHAR(100) NOT NULL,
            [TotalAmount] DECIMAL(18,2) NOT NULL,
            [TotalStudents] INT NOT NULL,
            [SubmittedBy] NVARCHAR(450) NOT NULL,
            [SubmittedDate] DATETIME2(7) NOT NULL DEFAULT (GETDATE()),
            [Status] NVARCHAR(20) NOT NULL DEFAULT ('Pending'),
            [ValidatedBy] NVARCHAR(450) NULL,
            [ValidationDate] DATETIME2(7) NULL,
            [ValidationNotes] NVARCHAR(1000) NULL,
            [RejectionReason] NVARCHAR(1000) NULL,
            [AcademicYear] NVARCHAR(20) NULL,
            [CreatedAt] DATETIME2(7) NOT NULL DEFAULT (GETDATE()),
            [UpdatedAt] DATETIME2(7) NULL,
            
            CONSTRAINT [PK_Remittances] PRIMARY KEY CLUSTERED ([RemittanceId] ASC)
                WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, 
                      ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
        );
        
        PRINT '    ✓ Table created successfully';
        
        -- Create unique constraint on BatchCode
        PRINT '3.2: Creating unique constraint on BatchCode...';
        ALTER TABLE [dbo].[Remittances] 
        ADD CONSTRAINT [UQ_Remittances_BatchCode] UNIQUE NONCLUSTERED ([BatchCode] ASC);
        PRINT '    ✓ Unique constraint created';
        
        -- Create indexes
        PRINT '3.3: Creating indexes...';
        CREATE NONCLUSTERED INDEX [IX_Remittances_Status]
        ON [dbo].[Remittances] ([Status] ASC);
        
        CREATE NONCLUSTERED INDEX [IX_Remittances_Section]
        ON [dbo].[Remittances] ([Section] ASC);
        
        CREATE NONCLUSTERED INDEX [IX_Remittances_SubmittedBy]
        ON [dbo].[Remittances] ([SubmittedBy] ASC);
        
        PRINT '    ✓ Indexes created';
        
        -- Create foreign keys
        PRINT '3.4: Creating foreign keys...';
        ALTER TABLE [dbo].[Remittances]  
        WITH CHECK ADD CONSTRAINT [FK_Remittances_SubmittedBy] 
        FOREIGN KEY([SubmittedBy])
        REFERENCES [dbo].[Student] ([StudentNum])
        ON DELETE NO ACTION;
        
        ALTER TABLE [dbo].[Remittances] 
        CHECK CONSTRAINT [FK_Remittances_SubmittedBy];
        
        ALTER TABLE [dbo].[Remittances]  
        WITH CHECK ADD CONSTRAINT [FK_Remittances_ValidatedBy] 
        FOREIGN KEY([ValidatedBy])
        REFERENCES [dbo].[Student] ([StudentNum])
        ON DELETE NO ACTION;
        
        ALTER TABLE [dbo].[Remittances] 
        CHECK CONSTRAINT [FK_Remittances_ValidatedBy];
        
        PRINT '    ✓ Foreign keys created';
    END
    ELSE
    BEGIN
        PRINT '3.1: Remittances table already exists - SKIPPED';
    END
    PRINT '';

    -- =====================================================================
    -- SECTION 4: CREATE REMITTANCEITEMS TABLE
    -- =====================================================================
    PRINT '========================================================================';
    PRINT 'SECTION 4: RemittanceItems Table';
    PRINT '========================================================================';
    PRINT '';
    
    IF NOT EXISTS (SELECT * FROM sys.objects 
                   WHERE object_id = OBJECT_ID(N'[dbo].[RemittanceItems]') 
                   AND type in (N'U'))
    BEGIN
        PRINT '4.1: Creating RemittanceItems table...';
        
        CREATE TABLE [dbo].[RemittanceItems] (
            [RemittanceItemId] INT IDENTITY(1,1) NOT NULL,
            [RemittanceId] INT NOT NULL,
            [FeeId] INT NULL,
            [FineId] INT NULL,
            [StudentNum] NVARCHAR(450) NOT NULL,
            [StudentName] NVARCHAR(300) NULL,
            [Amount] DECIMAL(18,2) NOT NULL,
            [CollectionDate] DATETIME2(7) NOT NULL,
            [PaymentMethod] NVARCHAR(50) NULL,
            [TransactionRef] NVARCHAR(100) NULL,
            [Notes] NVARCHAR(500) NULL,
            
            CONSTRAINT [PK_RemittanceItems] PRIMARY KEY CLUSTERED ([RemittanceItemId] ASC)
                WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, 
                      ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF)
        );
        
        PRINT '    ✓ Table created successfully';
        
        -- Create indexes
        PRINT '4.2: Creating indexes...';
        CREATE NONCLUSTERED INDEX [IX_RemittanceItems_RemittanceId]
        ON [dbo].[RemittanceItems] ([RemittanceId] ASC);
        
        CREATE NONCLUSTERED INDEX [IX_RemittanceItems_StudentNum]
        ON [dbo].[RemittanceItems] ([StudentNum] ASC);
        
        CREATE NONCLUSTERED INDEX [IX_RemittanceItems_FeeId]
        ON [dbo].[RemittanceItems] ([FeeId] ASC);
        
        CREATE NONCLUSTERED INDEX [IX_RemittanceItems_FineId]
        ON [dbo].[RemittanceItems] ([FineId] ASC);
        
        PRINT '    ✓ Indexes created';
        
        -- Create foreign keys
        PRINT '4.3: Creating foreign keys...';
        ALTER TABLE [dbo].[RemittanceItems]  
        WITH CHECK ADD CONSTRAINT [FK_RemittanceItems_Remittances] 
        FOREIGN KEY([RemittanceId])
        REFERENCES [dbo].[Remittances] ([RemittanceId])
        ON DELETE CASCADE;
        
        ALTER TABLE [dbo].[RemittanceItems] 
        CHECK CONSTRAINT [FK_RemittanceItems_Remittances];
        
        ALTER TABLE [dbo].[RemittanceItems]  
        WITH CHECK ADD CONSTRAINT [FK_RemittanceItems_Student] 
        FOREIGN KEY([StudentNum])
        REFERENCES [dbo].[Student] ([StudentNum])
        ON DELETE NO ACTION;
        
        ALTER TABLE [dbo].[RemittanceItems] 
        CHECK CONSTRAINT [FK_RemittanceItems_Student];
        
        ALTER TABLE [dbo].[RemittanceItems]  
        WITH CHECK ADD CONSTRAINT [FK_RemittanceItems_Fees] 
        FOREIGN KEY([FeeId])
        REFERENCES [dbo].[Fees] ([FeeId])
        ON DELETE NO ACTION;
        
        ALTER TABLE [dbo].[RemittanceItems] 
        CHECK CONSTRAINT [FK_RemittanceItems_Fees];
        
        ALTER TABLE [dbo].[RemittanceItems]  
        WITH CHECK ADD CONSTRAINT [FK_RemittanceItems_Fines] 
        FOREIGN KEY([FineId])
        REFERENCES [dbo].[Fines] ([FineId])
        ON DELETE NO ACTION;
        
        ALTER TABLE [dbo].[RemittanceItems] 
        CHECK CONSTRAINT [FK_RemittanceItems_Fines];
        
        PRINT '    ✓ Foreign keys created';
    END
    ELSE
    BEGIN
        PRINT '4.1: RemittanceItems table already exists - SKIPPED';
    END
    PRINT '';

    -- =====================================================================
    -- SECTION 5: ADD REMITTANCE TRACKING COLUMNS TO FEES TABLE
    -- =====================================================================
    PRINT '========================================================================';
    PRINT 'SECTION 5: Fees Table - Remittance Tracking Columns';
    PRINT '========================================================================';
    PRINT '';
    
    -- AmountPaid column
    IF NOT EXISTS (SELECT * FROM sys.columns 
                   WHERE object_id = OBJECT_ID('dbo.Fees') 
                   AND name = 'AmountPaid')
    BEGIN
        PRINT '5.1: Adding AmountPaid column to Fees table...';
        ALTER TABLE [dbo].[Fees]
        ADD [AmountPaid] DECIMAL(18,2) NOT NULL DEFAULT (0);
        PRINT '    ✓ AmountPaid column added';
    END
    ELSE
    BEGIN
        PRINT '5.1: AmountPaid column already exists - SKIPPED';
    END
    
    -- RemittanceStatus column
    IF NOT EXISTS (SELECT * FROM sys.columns 
                   WHERE object_id = OBJECT_ID('dbo.Fees') 
                   AND name = 'RemittanceStatus')
    BEGIN
        PRINT '5.2: Adding RemittanceStatus column to Fees table...';
        ALTER TABLE [dbo].[Fees]
        ADD [RemittanceStatus] NVARCHAR(20) NOT NULL DEFAULT ('NotRemitted');
        PRINT '    ✓ RemittanceStatus column added';
        
        -- Create index
        CREATE NONCLUSTERED INDEX [IX_Fees_RemittanceStatus]
        ON [dbo].[Fees] ([RemittanceStatus] ASC);
        PRINT '    ✓ Index on RemittanceStatus created';
    END
    ELSE
    BEGIN
        PRINT '5.2: RemittanceStatus column already exists - SKIPPED';
    END
    
    -- RemittanceId column
    IF NOT EXISTS (SELECT * FROM sys.columns 
                   WHERE object_id = OBJECT_ID('dbo.Fees') 
                   AND name = 'RemittanceId')
    BEGIN
        PRINT '5.3: Adding RemittanceId column to Fees table...';
        ALTER TABLE [dbo].[Fees]
        ADD [RemittanceId] INT NULL;
        PRINT '    ✓ RemittanceId column added';
        
        -- Create foreign key (only if Remittances table exists)
        IF EXISTS (SELECT * FROM sys.objects 
                   WHERE object_id = OBJECT_ID(N'[dbo].[Remittances]') 
                   AND type in (N'U'))
        BEGIN
            ALTER TABLE [dbo].[Fees]  
            WITH CHECK ADD CONSTRAINT [FK_Fees_Remittances] 
            FOREIGN KEY([RemittanceId])
            REFERENCES [dbo].[Remittances] ([RemittanceId])
            ON DELETE SET NULL;
            
            ALTER TABLE [dbo].[Fees] 
            CHECK CONSTRAINT [FK_Fees_Remittances];
            PRINT '    ✓ Foreign key to Remittances created';
        END
    END
    ELSE
    BEGIN
        PRINT '5.3: RemittanceId column already exists - SKIPPED';
    END
    
    -- CollectedBy column
    IF NOT EXISTS (SELECT * FROM sys.columns 
                   WHERE object_id = OBJECT_ID('dbo.Fees') 
                   AND name = 'CollectedBy')
    BEGIN
        PRINT '5.4: Adding CollectedBy column to Fees table...';
        ALTER TABLE [dbo].[Fees]
        ADD [CollectedBy] NVARCHAR(450) NULL;
        PRINT '    ✓ CollectedBy column added';
        
        -- Create foreign key
        ALTER TABLE [dbo].[Fees]  
        WITH CHECK ADD CONSTRAINT [FK_Fees_CollectedBy] 
        FOREIGN KEY([CollectedBy])
        REFERENCES [dbo].[Student] ([StudentNum])
        ON DELETE NO ACTION;
        
        ALTER TABLE [dbo].[Fees] 
        CHECK CONSTRAINT [FK_Fees_CollectedBy];
        PRINT '    ✓ Foreign key for CollectedBy created';
    END
    ELSE
    BEGIN
        PRINT '5.4: CollectedBy column already exists - SKIPPED';
    END
    
    -- CollectionDate column
    IF NOT EXISTS (SELECT * FROM sys.columns 
                   WHERE object_id = OBJECT_ID('dbo.Fees') 
                   AND name = 'CollectionDate')
    BEGIN
        PRINT '5.5: Adding CollectionDate column to Fees table...';
        ALTER TABLE [dbo].[Fees]
        ADD [CollectionDate] DATETIME2(7) NULL;
        PRINT '    ✓ CollectionDate column added';
    END
    ELSE
    BEGIN
        PRINT '5.5: CollectionDate column already exists - SKIPPED';
    END
    
    -- OfficialPaymentDate column
    IF NOT EXISTS (SELECT * FROM sys.columns 
                   WHERE object_id = OBJECT_ID('dbo.Fees') 
                   AND name = 'OfficialPaymentDate')
    BEGIN
        PRINT '5.6: Adding OfficialPaymentDate column to Fees table...';
        ALTER TABLE [dbo].[Fees]
        ADD [OfficialPaymentDate] DATETIME2(7) NULL;
        PRINT '    ✓ OfficialPaymentDate column added';
    END
    ELSE
    BEGIN
        PRINT '5.6: OfficialPaymentDate column already exists - SKIPPED';
    END
    PRINT '';

    -- =====================================================================
    -- SECTION 6: ADD REMITTANCE TRACKING COLUMNS TO FINES TABLE
    -- =====================================================================
    PRINT '========================================================================';
    PRINT 'SECTION 6: Fines Table - Remittance Tracking Columns';
    PRINT '========================================================================';
    PRINT '';
    
    -- AmountPaid column
    IF NOT EXISTS (SELECT * FROM sys.columns 
                   WHERE object_id = OBJECT_ID('dbo.Fines') 
                   AND name = 'AmountPaid')
    BEGIN
        PRINT '6.1: Adding AmountPaid column to Fines table...';
        ALTER TABLE [dbo].[Fines]
        ADD [AmountPaid] DECIMAL(18,2) NOT NULL DEFAULT (0);
        PRINT '    ✓ AmountPaid column added';
    END
    ELSE
    BEGIN
        PRINT '6.1: AmountPaid column already exists - SKIPPED';
    END
    
    -- RemittanceStatus column
    IF NOT EXISTS (SELECT * FROM sys.columns 
                   WHERE object_id = OBJECT_ID('dbo.Fines') 
                   AND name = 'RemittanceStatus')
    BEGIN
        PRINT '6.2: Adding RemittanceStatus column to Fines table...';
        ALTER TABLE [dbo].[Fines]
        ADD [RemittanceStatus] NVARCHAR(20) NOT NULL DEFAULT ('NotRemitted');
        PRINT '    ✓ RemittanceStatus column added';
        
        -- Create index
        CREATE NONCLUSTERED INDEX [IX_Fines_RemittanceStatus]
        ON [dbo].[Fines] ([RemittanceStatus] ASC);
        PRINT '    ✓ Index on RemittanceStatus created';
    END
    ELSE
    BEGIN
        PRINT '6.2: RemittanceStatus column already exists - SKIPPED';
    END
    
    -- RemittanceId column
    IF NOT EXISTS (SELECT * FROM sys.columns 
                   WHERE object_id = OBJECT_ID('dbo.Fines') 
                   AND name = 'RemittanceId')
    BEGIN
        PRINT '6.3: Adding RemittanceId column to Fines table...';
        ALTER TABLE [dbo].[Fines]
        ADD [RemittanceId] INT NULL;
        PRINT '    ✓ RemittanceId column added';
        
        -- Create foreign key (only if Remittances table exists)
        IF EXISTS (SELECT * FROM sys.objects 
                   WHERE object_id = OBJECT_ID(N'[dbo].[Remittances]') 
                   AND type in (N'U'))
        BEGIN
            ALTER TABLE [dbo].[Fines]  
            WITH CHECK ADD CONSTRAINT [FK_Fines_Remittances] 
            FOREIGN KEY([RemittanceId])
            REFERENCES [dbo].[Remittances] ([RemittanceId])
            ON DELETE SET NULL;
            
            ALTER TABLE [dbo].[Fines] 
            CHECK CONSTRAINT [FK_Fines_Remittances];
            PRINT '    ✓ Foreign key to Remittances created';
        END
    END
    ELSE
    BEGIN
        PRINT '6.3: RemittanceId column already exists - SKIPPED';
    END
    
    -- CollectedBy column
    IF NOT EXISTS (SELECT * FROM sys.columns 
                   WHERE object_id = OBJECT_ID('dbo.Fines') 
                   AND name = 'CollectedBy')
    BEGIN
        PRINT '6.4: Adding CollectedBy column to Fines table...';
        ALTER TABLE [dbo].[Fines]
        ADD [CollectedBy] NVARCHAR(450) NULL;
        PRINT '    ✓ CollectedBy column added';
        
        -- Create foreign key
        ALTER TABLE [dbo].[Fines]  
        WITH CHECK ADD CONSTRAINT [FK_Fines_CollectedBy] 
        FOREIGN KEY([CollectedBy])
        REFERENCES [dbo].[Student] ([StudentNum])
        ON DELETE NO ACTION;
        
        ALTER TABLE [dbo].[Fines] 
        CHECK CONSTRAINT [FK_Fines_CollectedBy];
        PRINT '    ✓ Foreign key for CollectedBy created';
    END
    ELSE
    BEGIN
        PRINT '6.4: CollectedBy column already exists - SKIPPED';
    END
    
    -- CollectionDate column
    IF NOT EXISTS (SELECT * FROM sys.columns 
                   WHERE object_id = OBJECT_ID('dbo.Fines') 
                   AND name = 'CollectionDate')
    BEGIN
        PRINT '6.5: Adding CollectionDate column to Fines table...';
        ALTER TABLE [dbo].[Fines]
        ADD [CollectionDate] DATETIME2(7) NULL;
        PRINT '    ✓ CollectionDate column added';
    END
    ELSE
    BEGIN
        PRINT '6.5: CollectionDate column already exists - SKIPPED';
    END
    
    -- OfficialPaymentDate column
    IF NOT EXISTS (SELECT * FROM sys.columns 
                   WHERE object_id = OBJECT_ID('dbo.Fines') 
                   AND name = 'OfficialPaymentDate')
    BEGIN
        PRINT '6.6: Adding OfficialPaymentDate column to Fines table...';
        ALTER TABLE [dbo].[Fines]
        ADD [OfficialPaymentDate] DATETIME2(7) NULL;
        PRINT '    ✓ OfficialPaymentDate column added';
    END
    ELSE
    BEGIN
        PRINT '6.6: OfficialPaymentDate column already exists - SKIPPED';
    END
    PRINT '';

    -- =====================================================================
    -- FINAL SUMMARY
    -- =====================================================================
    PRINT '========================================================================';
    PRINT 'MIGRATION COMPLETED SUCCESSFULLY!';
    PRINT '========================================================================';
    PRINT '';
    PRINT 'Summary of changes:';
    PRINT '  ✓ Announcements: ExpiryDate column added';
    PRINT '  ✓ UserAnnouncementDismissals: Table created with indexes and FKs';
    PRINT '  ✓ Remittances: Table created with indexes and FKs';
    PRINT '  ✓ RemittanceItems: Table created with indexes and FKs';
    PRINT '  ✓ Fees: Remittance tracking columns added';
    PRINT '  ✓ Fines: Remittance tracking columns added';
    PRINT '';
    PRINT 'Your database is now in sync with your Entity Framework models!';
    PRINT '';
    PRINT '========================================================================';
    PRINT 'Completed at: ' + CONVERT(VARCHAR(50), GETDATE(), 120);
    PRINT '========================================================================';
    PRINT '';
    
    COMMIT TRANSACTION;
    PRINT '✓ Transaction committed successfully';
    PRINT '';

END TRY
BEGIN CATCH
    SET @ErrorOccurred = 1;
    SET @ErrorMessage = ERROR_MESSAGE();
    
    PRINT '';
    PRINT '========================================================================';
    PRINT 'ERROR OCCURRED!';
    PRINT '========================================================================';
    PRINT 'Error Message: ' + @ErrorMessage;
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR(10));
    PRINT 'Error Procedure: ' + ISNULL(ERROR_PROCEDURE(), 'N/A');
    PRINT '';
    PRINT 'Rolling back transaction...';
    PRINT '========================================================================';
    
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    
    PRINT '✓ Transaction rolled back';
    PRINT '';
    PRINT 'Please review the error and try again.';
    PRINT '';
    
    -- Re-throw the error
    THROW;
END CATCH;

GO

PRINT '';
PRINT 'Script execution completed.';
PRINT '';
GO
