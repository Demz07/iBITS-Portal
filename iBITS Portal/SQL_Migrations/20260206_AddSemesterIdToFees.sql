/*
SQL Migration: Add SemesterId to Fees and link to Semesters
Date: 2026-02-06
Safe to re-run: YES (guards included)
Target: SQL Server
*/

SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRAN;

    -- 1) Add column if missing
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns c
        JOIN sys.tables t ON t.object_id = c.object_id
        JOIN sys.schemas s ON s.schema_id = t.schema_id
        WHERE s.name = 'dbo' AND t.name = 'Fees' AND c.name = 'SemesterId')
    BEGIN
        ALTER TABLE [dbo].[Fees]
        ADD [SemesterId] INT NULL;
    END

    -- 2) Add foreign key if missing
    IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE [name] = 'FK_Fees_Semester')
    BEGIN
        ALTER TABLE [dbo].[Fees] WITH CHECK
        ADD CONSTRAINT [FK_Fees_Semester]
        FOREIGN KEY ([SemesterId]) REFERENCES [dbo].[Semesters]([SemesterId])
        ON DELETE SET NULL;
    END

    -- 3) Indexes
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes i
        JOIN sys.tables t ON t.object_id = i.object_id
        JOIN sys.schemas s ON s.schema_id = t.schema_id
        WHERE s.name = 'dbo' AND t.name = 'Fees' AND i.name = 'IX_Fees_SemesterId')
    BEGIN
        CREATE INDEX [IX_Fees_SemesterId] ON [dbo].[Fees]([SemesterId]);
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes i
        JOIN sys.tables t ON t.object_id = i.object_id
        JOIN sys.schemas s ON s.schema_id = t.schema_id
        WHERE s.name = 'dbo' AND t.name = 'Fees' AND i.name = 'IX_Fees_StudentNum')
    BEGIN
        CREATE INDEX [IX_Fees_StudentNum] ON [dbo].[Fees]([StudentNum]);
    END

    COMMIT TRAN;
END TRY
BEGIN CATCH
    IF (XACT_STATE()) <> 0 ROLLBACK TRAN;
    DECLARE @Err nvarchar(max) = ERROR_MESSAGE();
    RAISERROR('Migration 20260206_AddSemesterIdToFees failed: %s', 16, 1, @Err);
END CATCH;
