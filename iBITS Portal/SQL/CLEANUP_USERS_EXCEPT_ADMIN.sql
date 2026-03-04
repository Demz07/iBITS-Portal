-- ============================================================
-- 1. DELETE ALL USERS EXCEPT ADMIN
-- ============================================================
BEGIN TRANSACTION;
BEGIN TRY
    -- Define Admin Identity
    DECLARE @AdminUsername NVARCHAR(256) = 'admin';
    DECLARE @AdminEmail NVARCHAR(256) = 'admin@ibits.edu.ph';

    -- Identify users to delete
    DECLARE @UserIds TABLE (Id NVARCHAR(450));
    INSERT INTO @UserIds 
    SELECT Id FROM AspNetUsers 
    WHERE UserName <> @AdminUsername AND Email <> @AdminEmail;

    -- Delete from Identity tables
    DELETE FROM AspNetUserRoles WHERE UserId IN (SELECT Id FROM @UserIds);
    DELETE FROM AspNetUserClaims WHERE UserId IN (SELECT Id FROM @UserIds);
    DELETE FROM AspNetUserLogins WHERE UserId IN (SELECT Id FROM @UserIds);
    DELETE FROM AspNetUserTokens WHERE UserId IN (SELECT Id FROM @UserIds);
    
    -- Delete from AspNetUsers
    DELETE FROM AspNetUsers WHERE Id IN (SELECT Id FROM @UserIds);

    COMMIT TRANSACTION;
    SELECT 'Users Cleanup' as [Operation], 'Success' as [Status], @@ROWCOUNT as [Rows Affected];
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    SELECT 'Users Cleanup' as [Operation], 'Failed' as [Status], ERROR_MESSAGE() as [Message];
END CATCH
