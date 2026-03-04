-- ============================================================
-- 4. DELETE ALL EVENTS (WITH IDENTITY RESET)
-- ============================================================
BEGIN TRANSACTION;
BEGIN TRY
    -- Attendance must be deleted before Events
    DELETE FROM [dbo].[Attendance];
    DELETE FROM [dbo].[Event];

    -- Reset Identity Counters to start from 1
    DBCC CHECKIDENT ('[dbo].[Event]', RESEED, 0);
    DBCC CHECKIDENT ('[dbo].[Attendance]', RESEED, 0);

    COMMIT TRANSACTION;
    SELECT 'Events Cleanup' as [Operation], 'Success' as [Status], 'IDs Reset to 1' as [Note];
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    SELECT 'Events Cleanup' as [Operation], 'Failed' as [Status], ERROR_MESSAGE() as [Message];
END CATCH
