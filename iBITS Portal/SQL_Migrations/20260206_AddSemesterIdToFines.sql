/*
SQL Migration: Add SemesterId to Fines and link to Semesters
Date: 2026-02-06
Safe to re-run: YES (guards included)
Target: SQL Server

What this does
- Adds column [SemesterId] INT NULL to [dbo].[Fines]
- Adds FK [FK_Fines_Semester] to [dbo].[Semesters]([SemesterId]) ON DELETE SET NULL
- Adds helpful indexes
- Optional backfill from Attendance (commented by default)
*/

SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRAN;

    ------------------------------------------------------------
    -- 1) Add column if missing
    ------------------------------------------------------------
    IF NOT EXISTS (
        SELECT 1
        FROM sys.columns c
        JOIN sys.tables t ON t.object_id = c.object_id
        JOIN sys.schemas s ON s.schema_id = t.schema_id
        WHERE s.name = 'dbo' AND t.name = 'Fines' AND c.name = 'SemesterId'
    )
    BEGIN
        ALTER TABLE [dbo].[Fines]
        ADD [SemesterId] INT NULL;
    END

    ------------------------------------------------------------
    -- 2) Add foreign key if missing
    ------------------------------------------------------------
    IF NOT EXISTS (
        SELECT 1
        FROM sys.foreign_keys fk
        WHERE fk.[name] = 'FK_Fines_Semester'
    )
    BEGIN
        ALTER TABLE [dbo].[Fines] WITH CHECK
        ADD CONSTRAINT [FK_Fines_Semester]
        FOREIGN KEY ([SemesterId]) REFERENCES [dbo].[Semesters]([SemesterId])
        ON DELETE SET NULL;
    END

    ------------------------------------------------------------
    -- 3) Add indexes if missing
    ------------------------------------------------------------
    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes i
        JOIN sys.tables t ON t.object_id = i.object_id
        JOIN sys.schemas s ON s.schema_id = t.schema_id
        WHERE s.name = 'dbo' AND t.name = 'Fines' AND i.name = 'IX_Fines_SemesterId'
    )
    BEGIN
        CREATE INDEX [IX_Fines_SemesterId] ON [dbo].[Fines]([SemesterId]);
    END

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes i
        JOIN sys.tables t ON t.object_id = i.object_id
        JOIN sys.schemas s ON s.schema_id = t.schema_id
        WHERE s.name = 'dbo' AND t.name = 'Fines' AND i.name = 'IX_Fines_StudentNum'
    )
    BEGIN
        CREATE INDEX [IX_Fines_StudentNum] ON [dbo].[Fines]([StudentNum]);
    END

    ------------------------------------------------------------
    -- 4) OPTIONAL: Backfill SemesterId from Attendance (if linked)
    --    Uncomment if you want to populate existing fines using Attendance.SemesterId
    ------------------------------------------------------------
    /*
    UPDATE f
    SET f.[SemesterId] = a.[SemesterId]
    FROM [dbo].[Fines] f
    INNER JOIN [dbo].[Attendance] a ON a.[AttendanceId] = f.[AttendanceId]
    WHERE f.[SemesterId] IS NULL AND a.[SemesterId] IS NOT NULL;
    */

    COMMIT TRAN;
END TRY
BEGIN CATCH
    IF (XACT_STATE()) <> 0 ROLLBACK TRAN;
    DECLARE @Err nvarchar(max) = ERROR_MESSAGE();
    RAISERROR('Migration 20260206_AddSemesterIdToFines failed: %s', 16, 1, @Err);
END CATCH;
