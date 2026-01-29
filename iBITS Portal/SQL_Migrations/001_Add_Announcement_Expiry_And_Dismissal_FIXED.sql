-- ============================================================
-- FILE PATH: SQL_Migrations/001_Add_Announcement_Expiry_And_Dismissal_FIXED.sql
-- ============================================================
-- CORRECTED Migration Script for Payment Reminders Optimization
-- Adds expiry date to Announcements and creates dismissal tracking
-- ============================================================

-- Step 1: Add ExpiryDate column to Announcements table (if not exists)
-- ============================================================
IF NOT EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Announcements' 
    AND COLUMN_NAME = 'ExpiryDate'
)
BEGIN
    ALTER TABLE Announcements
    ADD ExpiryDate DATETIME NULL;
    
    PRINT 'ExpiryDate column added to Announcements table';
END
ELSE
BEGIN
    PRINT 'ExpiryDate column already exists in Announcements table';
END
GO

-- Step 2: Create UserAnnouncementDismissals table
-- ============================================================
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'UserAnnouncementDismissals')
BEGIN
    CREATE TABLE UserAnnouncementDismissals (
        Id INT PRIMARY KEY IDENTITY(1,1),
        StudentNum NVARCHAR(450) NOT NULL,
        AnnouncementId INT NOT NULL,
        DismissedAt DATETIME NOT NULL DEFAULT GETDATE(),
        
        -- Foreign Keys (CORRECTED: Student not Students)
        CONSTRAINT FK_UserAnnouncementDismissal_Student 
            FOREIGN KEY (StudentNum) REFERENCES Student(StudentNum) ON DELETE CASCADE,
        CONSTRAINT FK_UserAnnouncementDismissal_Announcement 
            FOREIGN KEY (AnnouncementId) REFERENCES Announcements(Id) ON DELETE CASCADE,
            
        -- Unique constraint to prevent duplicate dismissals
        CONSTRAINT UQ_StudentAnnouncement 
            UNIQUE (StudentNum, AnnouncementId)
    );
    
    -- Create indexes for performance
    CREATE INDEX IX_UserAnnouncementDismissals_StudentNum 
        ON UserAnnouncementDismissals(StudentNum);
    CREATE INDEX IX_UserAnnouncementDismissals_AnnouncementId 
        ON UserAnnouncementDismissals(AnnouncementId);
    
    PRINT 'UserAnnouncementDismissals table created successfully';
END
ELSE
BEGIN
    PRINT 'UserAnnouncementDismissals table already exists';
END
GO

-- Step 3: Update existing announcements with default expiry (optional)
-- ============================================================
-- Set expiry date to 30 days from now for existing payment reminders without expiry
UPDATE Announcements
SET ExpiryDate = DATEADD(DAY, 30, GETDATE())
WHERE ExpiryDate IS NULL 
  AND AnnouncementType = 'Payment Reminder';

PRINT 'Existing payment reminders updated with 30-day expiry';
GO

-- Step 4: Verification Queries
-- ============================================================
PRINT '========================================';
PRINT 'VERIFICATION RESULTS:';
PRINT '========================================';

SELECT 
    'Announcements with ExpiryDate' as TableInfo,
    COUNT(*) as RecordCount
FROM Announcements
WHERE ExpiryDate IS NOT NULL;

SELECT 
    'UserAnnouncementDismissals Table' as TableInfo,
    COUNT(*) as RecordCount
FROM UserAnnouncementDismissals;

-- Verify table structure
SELECT 
    'UserAnnouncementDismissals Columns' as Info,
    COLUMN_NAME, 
    DATA_TYPE, 
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'UserAnnouncementDismissals'
ORDER BY ORDINAL_POSITION;

-- Verify foreign keys
SELECT 
    'Foreign Keys' as Info,
    OBJECT_NAME(fk.parent_object_id) AS TableName,
    fk.name AS ForeignKeyName,
    OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable
FROM sys.foreign_keys fk
WHERE OBJECT_NAME(fk.parent_object_id) = 'UserAnnouncementDismissals';

PRINT '========================================';
PRINT 'Migration completed successfully!';
PRINT '========================================';
GO
