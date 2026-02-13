-- ============================================================
-- MIGRATION: Add Program Column to Remittances Table
-- DATE: 2026-02-13
-- DESCRIPTION: Adds Program column to store program info (e.g., BSIT, DIT)
-- ============================================================

USE [PortaliBITS];
GO

BEGIN TRANSACTION;

BEGIN TRY
    -- Step 1: Check if column exists
    IF NOT EXISTS (
        SELECT * FROM sys.columns 
        WHERE object_id = OBJECT_ID(N'[dbo].[Remittances]') 
        AND name = 'Program'
    )
    BEGIN
        -- Add the Program column to Remittances table
        ALTER TABLE [dbo].[Remittances]
        ADD [Program] NVARCHAR(50) NULL;
        
        PRINT 'Program column added successfully.';
        
        -- Step 2: Populate existing records with Program extracted from Section
        -- Extract the first part before space from Section (e.g., "BSIT 3-1" -> "BSIT")
        UPDATE [dbo].[Remittances]
        SET [Program] = 
            CASE 
                WHEN CHARINDEX(' ', [Section]) > 0 
                THEN LEFT([Section], CHARINDEX(' ', [Section]) - 1)
                ELSE [Section]
            END
        WHERE [Section] IS NOT NULL;
        
        PRINT 'Existing records updated with Program values.';
        
        -- Step 3: Verify the changes
        SELECT 
            COUNT(*) AS TotalRemittances,
            COUNT([Program]) AS RemittancesWithProgram,
            COUNT(*) - COUNT([Program]) AS RemittancesWithoutProgram
        FROM [dbo].[Remittances];
        
        PRINT 'Migration completed successfully!';
    END
    ELSE
    BEGIN
        PRINT 'Program column already exists. Skipping migration.';
    END
    
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    
    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();
    
    PRINT 'ERROR: Migration failed!';
    RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;

-- ============================================================
-- ROLLBACK SCRIPT (if needed)
-- ============================================================
-- BEGIN TRANSACTION;
-- ALTER TABLE [dbo].[Remittances] DROP COLUMN [Program];
-- COMMIT TRANSACTION;
-- ============================================================
