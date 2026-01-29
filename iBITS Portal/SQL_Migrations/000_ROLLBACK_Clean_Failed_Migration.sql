-- ============================================================
-- ROLLBACK SCRIPT: Clean up failed migration attempt
-- ============================================================
-- Run this first to clean up any partial migration
-- ============================================================

-- Drop UserAnnouncementDismissals table if it exists (even partially)
IF OBJECT_ID('dbo.UserAnnouncementDismissals', 'U') IS NOT NULL
BEGIN
    DROP TABLE UserAnnouncementDismissals;
    PRINT 'Dropped UserAnnouncementDismissals table';
END
ELSE
BEGIN
    PRINT 'UserAnnouncementDismissals table does not exist';
END
GO

-- ExpiryDate column should remain (it was successfully added)
-- But if you want to remove it, uncomment the following:
/*
IF EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Announcements' 
    AND COLUMN_NAME = 'ExpiryDate'
)
BEGIN
    ALTER TABLE Announcements
    DROP COLUMN ExpiryDate;
    
    PRINT 'ExpiryDate column removed from Announcements table';
END
*/

PRINT 'Rollback completed. You can now run the corrected migration script.';
GO
