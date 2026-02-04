/***************************************************************************
 * Migration: Add UserAnnouncementDismissals Table
 * Created: 2026-02-03
 * 
 * Purpose: Create the UserAnnouncementDismissals table to track which 
 *          announcements have been dismissed by which students.
 * 
 * IMPORTANT: This fixes the pending migration error:
 *            "Invalid object name 'UserAnnouncementDismissals'"
 * 
 * HOW TO RUN:
 *   1. Open SQL Server Management Studio
 *   2. Connect to your database
 *   3. Run this script against the PortaliBITS database
 ***************************************************************************/

USE [PortaliBITS]
GO

SET NOCOUNT ON;
GO

PRINT '========================================================================';
PRINT 'Creating UserAnnouncementDismissals Table';
PRINT 'Started at: ' + CONVERT(VARCHAR(50), GETDATE(), 120);
PRINT '========================================================================';
PRINT '';

BEGIN TRANSACTION;

BEGIN TRY
    -- Check if table already exists
    IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserAnnouncementDismissals]') AND type in (N'U'))
    BEGIN
        PRINT '⚠ WARNING: Table [UserAnnouncementDismissals] already exists.';
        PRINT '   Skipping table creation.';
        PRINT '';
    END
    ELSE
    BEGIN
        PRINT 'Step 1: Creating [UserAnnouncementDismissals] table...';
        
        -- Create the UserAnnouncementDismissals table
        CREATE TABLE [dbo].[UserAnnouncementDismissals] (
            [Id] INT IDENTITY(1,1) NOT NULL,
            [StudentNum] NVARCHAR(450) NOT NULL,
            [AnnouncementId] INT NOT NULL,
            [DismissedAt] DATETIME2(7) NOT NULL DEFAULT (GETDATE()),
            
            CONSTRAINT [PK_UserAnnouncementDismissals] PRIMARY KEY CLUSTERED ([Id] ASC)
                WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, 
                      ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) 
                ON [PRIMARY]
        ) ON [PRIMARY];
        
        PRINT '   ✓ Table created successfully';
        PRINT '';
        
        -- Step 2: Create unique constraint to prevent duplicate dismissals
        PRINT 'Step 2: Creating unique constraint [UQ_StudentAnnouncement]...';
        
        ALTER TABLE [dbo].[UserAnnouncementDismissals] 
        ADD CONSTRAINT [UQ_StudentAnnouncement] UNIQUE NONCLUSTERED 
        (
            [StudentNum] ASC,
            [AnnouncementId] ASC
        )
        WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, 
              ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) 
        ON [PRIMARY];
        
        PRINT '   ✓ Unique constraint created successfully';
        PRINT '';
        
        -- Step 3: Create foreign key to Student table
        PRINT 'Step 3: Creating foreign key [FK_UserAnnouncementDismissal_Student]...';
        
        ALTER TABLE [dbo].[UserAnnouncementDismissals]  
        WITH CHECK ADD CONSTRAINT [FK_UserAnnouncementDismissal_Student] 
        FOREIGN KEY([StudentNum])
        REFERENCES [dbo].[Student] ([StudentNum])
        ON DELETE CASCADE;
        
        ALTER TABLE [dbo].[UserAnnouncementDismissals] 
        CHECK CONSTRAINT [FK_UserAnnouncementDismissal_Student];
        
        PRINT '   ✓ Foreign key to Student created successfully';
        PRINT '';
        
        -- Step 4: Create foreign key to Announcements table
        PRINT 'Step 4: Creating foreign key [FK_UserAnnouncementDismissal_Announcement]...';
        
        ALTER TABLE [dbo].[UserAnnouncementDismissals]  
        WITH CHECK ADD CONSTRAINT [FK_UserAnnouncementDismissal_Announcement] 
        FOREIGN KEY([AnnouncementId])
        REFERENCES [dbo].[Announcements] ([AnnouncementId])
        ON DELETE CASCADE;
        
        ALTER TABLE [dbo].[UserAnnouncementDismissals] 
        CHECK CONSTRAINT [FK_UserAnnouncementDismissal_Announcement];
        
        PRINT '   ✓ Foreign key to Announcements created successfully';
        PRINT '';
        
        -- Step 5: Create index on StudentNum for faster lookups
        PRINT 'Step 5: Creating index [IX_UserAnnouncementDismissals_StudentNum]...';
        
        CREATE NONCLUSTERED INDEX [IX_UserAnnouncementDismissals_StudentNum]
        ON [dbo].[UserAnnouncementDismissals] ([StudentNum] ASC)
        WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, 
              DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, 
              OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) 
        ON [PRIMARY];
        
        PRINT '   ✓ Index on StudentNum created successfully';
        PRINT '';
        
        -- Step 6: Create index on AnnouncementId for faster lookups
        PRINT 'Step 6: Creating index [IX_UserAnnouncementDismissals_AnnouncementId]...';
        
        CREATE NONCLUSTERED INDEX [IX_UserAnnouncementDismissals_AnnouncementId]
        ON [dbo].[UserAnnouncementDismissals] ([AnnouncementId] ASC)
        WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, 
              DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, 
              OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) 
        ON [PRIMARY];
        
        PRINT '   ✓ Index on AnnouncementId created successfully';
        PRINT '';
    END
    
    -- Verify table creation
    PRINT '========================================================================';
    PRINT 'VERIFICATION: Checking table structure...';
    PRINT '========================================================================';
    PRINT '';
    
    -- Show table columns
    SELECT 
        COLUMN_NAME,
        DATA_TYPE,
        CHARACTER_MAXIMUM_LENGTH,
        IS_NULLABLE,
        COLUMN_DEFAULT
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'UserAnnouncementDismissals'
    ORDER BY ORDINAL_POSITION;
    
    PRINT '';
    
    -- Show constraints
    PRINT 'Constraints:';
    SELECT 
        CONSTRAINT_NAME,
        CONSTRAINT_TYPE
    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS
    WHERE TABLE_NAME = 'UserAnnouncementDismissals';
    
    PRINT '';
    
    -- Show indexes
    PRINT 'Indexes:';
    SELECT 
        i.name AS IndexName,
        i.type_desc AS IndexType,
        COL_NAME(ic.object_id, ic.column_id) AS ColumnName
    FROM sys.indexes i
    INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
    WHERE i.object_id = OBJECT_ID('dbo.UserAnnouncementDismissals')
    ORDER BY i.name, ic.key_ordinal;
    
    PRINT '';
    PRINT '========================================================================';
    PRINT 'Migration completed successfully at: ' + CONVERT(VARCHAR(50), GETDATE(), 120);
    PRINT '========================================================================';
    PRINT '';
    PRINT '✓ UserAnnouncementDismissals table is ready to use!';
    PRINT '';
    PRINT 'Next steps:';
    PRINT '  1. Test your application - the error should be resolved';
    PRINT '  2. If you still see migration warnings, run:';
    PRINT '     dotnet ef migrations add SyncUserAnnouncementDismissals';
    PRINT '     (This will sync EF Core with the database)';
    PRINT '';
    
    COMMIT TRANSACTION;
    
END TRY
BEGIN CATCH
    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorLine INT = ERROR_LINE();
    
    PRINT '';
    PRINT '========================================================================';
    PRINT 'ERROR OCCURRED!';
    PRINT '========================================================================';
    PRINT 'Error Message: ' + @ErrorMessage;
    PRINT 'Error Line: ' + CAST(@ErrorLine AS VARCHAR(10));
    PRINT '';
    PRINT 'Rolling back transaction...';
    PRINT '========================================================================';
    
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;
    
    THROW;
END CATCH;

GO

PRINT '';
PRINT '========================================================================';
PRINT 'Script execution completed';
PRINT '========================================================================';
GO
