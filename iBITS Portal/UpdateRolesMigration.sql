/* 
   iBITS Portal - SAFE Role Migration Script (SQL SERVER VERSION)
   Phase 4: Transition from 'Officer' to 'Org Officer'
   ============================================================
   FEATURES: 
   - T-SQL Compatible (SQL Server)
   - Transactional Safety (Rollback on error)
   - Comparison Table (Before vs After)
   - Duplicate Prevention
*/

BEGIN TRY
    BEGIN TRANSACTION;

    -- 1. Create a temporary table to track the migration progress
    IF OBJECT_ID('tempdb..#MigrationReport') IS NOT NULL DROP TABLE #MigrationReport;
    
    CREATE TABLE #MigrationReport (
        Role_Name NVARCHAR(256),
        Before_Count INT DEFAULT 0,
        After_Count INT DEFAULT 0,
        Migration_Status NVARCHAR(MAX)
    );

    -- 2. Capture 'BEFORE' stats
    INSERT INTO #MigrationReport (Role_Name, Before_Count)
    SELECT r.Name, COUNT(ur.UserId)
    FROM AspNetRoles r
    LEFT JOIN AspNetUserRoles ur ON r.Id = ur.RoleId
    WHERE r.Name IN ('Officer', 'Org Officer', 'Class Officer')
    GROUP BY r.Name;

    -- 3. EXECUTE MIGRATION: Assign 'Org Officer' to all existing 'Officers'
    INSERT INTO AspNetUserRoles (UserId, RoleId)
    SELECT ur.UserId, (SELECT r.Id FROM AspNetRoles r WHERE r.Name = 'Org Officer')
    FROM AspNetUserRoles ur
    WHERE ur.RoleId = (SELECT r.Id FROM AspNetRoles r WHERE r.Name = 'Officer')
    -- Safety check: only insert if they don't already have the new role
    AND NOT EXISTS (
        SELECT 1 FROM AspNetUserRoles ur2 
        WHERE ur2.UserId = ur.UserId 
        AND ur2.RoleId = (SELECT r2.Id FROM AspNetRoles r2 WHERE r2.Name = 'Org Officer')
    );

    -- 4. Capture 'AFTER' stats and update the report
    UPDATE rep
    SET rep.After_Count = sub.new_count,
        rep.Migration_Status = CASE 
            WHEN sub.new_count > rep.Before_Count THEN 'SUCCESS: Users Migrated'
            WHEN sub.new_count = rep.Before_Count AND rep.Before_Count > 0 THEN 'SKIPPED: Already Migrated'
            ELSE 'NO ACTION: No users found'
        END
    FROM #MigrationReport rep
    CROSS APPLY (
        SELECT COUNT(ur.UserId) as new_count
        FROM AspNetRoles r
        LEFT JOIN AspNetUserRoles ur ON r.Id = ur.RoleId
        WHERE r.Name = rep.Role_Name
    ) sub;

    -- 5. DISPLAY FEEDBACK MESSAGE
    SELECT 'MIGRATION COMPLETE' AS [Feedback], 
           'All users with the "Officer" role have been safely granted the "Org Officer" role.' AS [Message];

    -- 6. DISPLAY COMPARISON TABLE
    SELECT 
        Role_Name AS [Role], 
        ISNULL(Before_Count, 0) AS [Count Before], 
        ISNULL(After_Count, 0) AS [Count After],
        Migration_Status AS [Status]
    FROM #MigrationReport
    ORDER BY Role_Name;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    
    SELECT 
        ERROR_NUMBER() AS ErrorNumber,
        ERROR_MESSAGE() AS ErrorMessage,
        'MIGRATION FAILED: Changes rolled back safely.' AS Status;
END CATCH;

-- Cleanup
IF OBJECT_ID('tempdb..#MigrationReport') IS NOT NULL DROP TABLE #MigrationReport;
