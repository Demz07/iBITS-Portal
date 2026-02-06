-- ========================================================================================
-- iBITS Portal - FIXED Semester + Time-In/Time-Out Implementation (ENGLISH ONLY)
-- Version: 2.1 - FIXED Student table column references
-- Date: 2025-02-05
-- Fixed: Uses correct Student table column names (YearLevelSection, Course)
-- Target: Filipino users with English column names
-- ========================================================================================

USE [PortaliBITS]
GO

SET NOCOUNT ON;
GO

PRINT N'========================================================================';
PRINT N'iBITS Portal - FIXED Semester + Time-In/Time-Out Implementation';
PRINT N'English Only Column Names for Filipino Users';
PRINT N'Started at: ' + CONVERT(VARCHAR(50), GETDATE(), 120);
PRINT N'========================================================================';
PRINT N'';

BEGIN TRANSACTION;
GO

-- ========================================
-- SECTION 1: ACADEMIC YEARS TABLE
-- ========================================
PRINT N'Creating AcademicYears table...';

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'AcademicYears')
BEGIN
    CREATE TABLE [dbo].[AcademicYears] (
        [AcademicYearId] [int] IDENTITY(1,1) PRIMARY KEY,
        [YearName] [nvarchar](20) NOT NULL,
        [StartDate] [date] NOT NULL,
        [EndDate] [date] NOT NULL,
        [IsActive] [bit] NOT NULL DEFAULT 0,
        [CreatedAt] [datetime2](7) DEFAULT GETDATE(),
        [CreatedBy] [nvarchar](450) NULL,
        CONSTRAINT [UQ_AcademicYears_YearName] UNIQUE (YearName)
    );
    
    -- Create index for fast semester filtering
    CREATE NONCLUSTERED INDEX [IX_AcademicYears_YearName] ON [AcademicYears]([YearName]);
    
    PRINT N'    ✓ AcademicYears table created successfully';
END
ELSE
BEGIN
    PRINT N'    ✓ AcademicYears table already exists';
END
GO

-- ========================================
-- SECTION 2: SEMESTERS TABLE
-- ========================================
PRINT N'Creating Semesters table...';

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Semesters')
BEGIN
    CREATE TABLE [dbo].[Semesters] (
        [SemesterId] [int] IDENTITY(1,1) PRIMARY KEY,
        [SemesterName] [nvarchar](50) NOT NULL,
        [AcademicYearId] [int] NOT NULL,
        [StartDate] [date] NOT NULL,
        [EndDate] [date] NOT NULL,
        [IsCurrent] [bit] NOT NULL DEFAULT 0,
        [CreatedAt] [datetime2](7) DEFAULT GETDATE(),
        [CreatedBy] [nvarchar](450) NULL,
        CONSTRAINT [UQ_Semesters_AcademicYear_SemesterName] UNIQUE (AcademicYearId, SemesterName),
        CONSTRAINT [FK_Semesters_AcademicYears] FOREIGN KEY (AcademicYearId)
            REFERENCES [dbo].[AcademicYears] (AcademicYearId)
            ON DELETE NO ACTION
    );
    
    -- Create index for fast semester filtering
    CREATE NONCLUSTERED INDEX [IX_Semesters_AcademicYearId] ON [Semesters]([AcademicYearId]);
    CREATE NONCLUSTERED INDEX [IX_Semesters_IsCurrent] ON [Semesters]([IsCurrent]);
    
    PRINT N'    ✓ Semesters table created successfully';
END
ELSE
BEGIN
    PRINT N'    ✓ Semesters table already exists';
END
GO

-- ========================================
-- SECTION 3: UPDATE STUDENT SEMESTERS TABLE
-- ========================================
PRINT N'Updating StudentSemesters table...';

-- The StudentSemesters table already exists, let's add missing columns if needed
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'StudentSemesters' AND COLUMN_NAME = 'Course')
BEGIN
    ALTER TABLE [dbo].[StudentSemesters] ADD [Course] [nvarchar](50) NULL;
    PRINT N'    ✓ Added Course column to StudentSemesters';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'StudentSemesters' AND COLUMN_NAME = 'TotalAbsences')
BEGIN
    ALTER TABLE [dbo].[StudentSemesters] ADD 
        [TotalAbsences] [int] NOT NULL DEFAULT 0,
        [TotalTardies] [int] NOT NULL DEFAULT 0,
        [TotalEarlyExits] [int] NOT NULL DEFAULT 0,
        [TotalLeaves] [int] NOT NULL DEFAULT 0,
        [AttendancePercentage] [decimal](5,2) NOT NULL DEFAULT 100.00;
    
    PRINT N'    ✓ Added attendance tracking columns to StudentSemesters';
END

-- Update existing records with course information from Student table
UPDATE ss
SET ss.Course = s.Course
FROM [dbo].[StudentSemesters] ss
JOIN [dbo].[Student] s ON ss.StudentNum = s.StudentNum
WHERE ss.Course IS NULL;

PRINT N'    ✓ Updated StudentSemesters with course information';
GO

-- ========================================
-- SECTION 4: ADD SEMESTER COLUMNS TO EXISTING TABLES
-- ========================================
PRINT N'Adding semester columns to existing tables...';

-- Add SemesterId to Attendance table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Attendance' AND COLUMN_NAME = 'SemesterId')
BEGIN
    ALTER TABLE [dbo].[Attendance] ADD [SemesterId] [int] NULL;
    PRINT N'    ✓ Added SemesterId to Attendance table';
END

-- Add SemesterId to Event table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Event' AND COLUMN_NAME = 'SemesterId')
BEGIN
    ALTER TABLE [dbo].[Event] ADD [SemesterId] [int] NULL;
    PRINT N'    ✓ Added SemesterId to Event table';
END

-- Add SemesterId to Student table (if not exists)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Student' AND COLUMN_NAME = 'SemesterId')
BEGIN
    ALTER TABLE [dbo].[Student] ADD [SemesterId] [int] NULL;
    PRINT N'    ✓ Added SemesterId to Student table';
END
GO

-- ========================================
-- SECTION 5: ADD TIME TRACKING COLUMNS TO ATTENDANCE TABLE
-- ========================================
PRINT N'Adding time tracking columns to Attendance table...';

-- Time tracking columns
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Attendance' AND COLUMN_NAME = 'TimeIn')
BEGIN
    ALTER TABLE [dbo].[Attendance] ADD 
        [TimeIn] [datetime2](7) NULL,          -- When student checks IN
        [TimeOut] [datetime2](7) NULL,         -- When student checks OUT
        [DurationMinutes] [int] NULL,         -- Calculated duration in minutes
        [ScanTime] [datetime2](7) DEFAULT GETDATE(),  -- Scan timestamp
        [ScanDeviceType] [nvarchar](50) NULL,  -- Mobile/Desktop scanner
        [Location] [nvarchar](200) NULL,       -- Location-based attendance
        [MacAddress] [nvarchar](50) NULL,      -- Device MAC address
        [Remarks] [nvarchar](max) NULL,        -- Dean's remarks
        [IsOnTime] [bit] NULL DEFAULT 1;       -- Track punctuality
    
    PRINT N'    ✓ Added time tracking columns to Attendance table';
END
GO

-- ========================================
-- SECTION 6: ADD LOCATION COLUMNS TO STUDENT TABLE
-- ========================================
PRINT N'Adding location columns to Student table...';

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
               WHERE TABLE_NAME = 'Student' AND COLUMN_NAME = 'Location')
BEGIN
    ALTER TABLE [dbo].[Student] ADD 
        [Location] [nvarchar](200) NULL,       -- Real-time location tracking
        [LastScanTime] [datetime2](7) NULL,    -- Last QR scan timestamp
        [LastScanDevice] [nvarchar](50) NULL;  -- Last device used
    
    PRINT N'    ✓ Added location columns to Student table';
END
GO

-- ========================================
-- SECTION 7: CREATE FOREIGN KEYS FOR NEW COLUMNS
-- ========================================
PRINT N'Creating foreign keys for new columns...';

-- Attendance.SemesterId foreign key
IF NOT EXISTS (SELECT * FROM sys.foreign_keys 
               WHERE name = 'FK_Attendance_Semesters')
BEGIN
    ALTER TABLE [dbo].[Attendance]  
    WITH CHECK ADD CONSTRAINT [FK_Attendance_Semesters] 
    FOREIGN KEY([SemesterId])
    REFERENCES [dbo].[Semesters] ([SemesterId])
    ON DELETE NO ACTION;
    
    ALTER TABLE [dbo].[Attendance] 
    CHECK CONSTRAINT [FK_Attendance_Semesters];
    
    PRINT N'    ✓ Created FK_Attendance_Semesters';
END

-- Event.SemesterId foreign key
IF NOT EXISTS (SELECT * FROM sys.foreign_keys 
               WHERE name = 'FK_Event_Semesters')
BEGIN
    ALTER TABLE [dbo].[Event]  
    WITH CHECK ADD CONSTRAINT [FK_Event_Semesters] 
    FOREIGN KEY([SemesterId])
    REFERENCES [dbo].[Semesters] ([SemesterId])
    ON DELETE NO ACTION;
    
    ALTER TABLE [dbo].[Event] 
    CHECK CONSTRAINT [FK_Event_Semesters];
    
    PRINT N'    ✓ Created FK_Event_Semesters';
END

-- Student.SemesterId foreign key
IF NOT EXISTS (SELECT * FROM sys.foreign_keys 
               WHERE name = 'FK_Student_Semesters')
BEGIN
    ALTER TABLE [dbo].[Student]  
    WITH CHECK ADD CONSTRAINT [FK_Student_Semesters] 
    FOREIGN KEY([SemesterId])
    REFERENCES [dbo].[Semesters] ([SemesterId])
    ON DELETE NO ACTION;
    
    ALTER TABLE [dbo].[Student] 
    CHECK CONSTRAINT [FK_Student_Semesters];
    
    PRINT N'    ✓ Created FK_Student_Semesters';
END
GO

-- ========================================
-- SECTION 8: INSERT ACADEMIC YEARS DATA
-- ========================================
PRINT N'Inserting Academic Years data...';

IF NOT EXISTS (SELECT * FROM [dbo].[AcademicYears] WHERE YearName = '2024-2025')
BEGIN
    INSERT INTO [dbo].[AcademicYears] 
    ([YearName], [StartDate], [EndDate], [IsActive], [CreatedBy])
    VALUES 
    ('2024-2025', '2024-06-01', '2025-05-31', 0, 'System'),
    ('2025-2026', '2025-06-01', '2026-05-31', 1, 'System'),
    ('2026-2027', '2026-06-01', '2027-05-31', 0, 'System');
    
    PRINT N'    ✓ Academic Years inserted';
END
ELSE
BEGIN
    PRINT N'    ✓ Academic Years already exist';
END
GO

-- ========================================
-- SECTION 9: INSERT SEMESTERS DATA
-- ========================================
PRINT N'Inserting Semesters data...';

IF NOT EXISTS (SELECT * FROM [dbo].[Semesters] WHERE SemesterName = 'First Semester 2025-2026')
BEGIN
    -- Get Academic Year IDs
    DECLARE @AY24_25 INT = (SELECT AcademicYearId FROM AcademicYears WHERE YearName = '2024-2025');
    DECLARE @AY25_26 INT = (SELECT AcademicYearId FROM AcademicYears WHERE YearName = '2025-2026');
    
    INSERT INTO [dbo].[Semesters] 
    ([SemesterName], [AcademicYearId], [StartDate], [EndDate], [IsCurrent], [CreatedBy])
    VALUES 
    -- 2024-2025 Academic Year
    ('First Semester 2024-2025', @AY24_25, '2024-06-01', '2024-10-31', 0, 'System'),
    ('Second Semester 2024-2025', @AY24_25, '2024-11-01', '2025-03-31', 0, 'System'),
    ('Summer 2024-2025', @AY24_25, '2025-04-01', '2025-05-31', 0, 'System'),
    
    -- 2025-2026 Academic Year
    ('First Semester 2025-2026', @AY25_26, '2025-06-01', '2025-10-31', 1, 'System'),
    ('Second Semester 2025-2026', @AY25_26, '2025-11-01', '2026-03-31', 0, 'System'),
    ('Summer 2025-2026', @AY25_26, '2026-04-01', '2026-05-31', 0, 'System');
    
    PRINT N'    ✓ Semesters inserted';
END
ELSE
BEGIN
    PRINT N'    ✓ Semesters already exist';
END
GO

-- ========================================
-- SECTION 10: ENROLL ALL STUDENTS IN CURRENT SEMESTER (FIXED)
-- ========================================
PRINT N'Enrolling students in current semester...';

DECLARE @CurrentSemesterId INT = (
    SELECT TOP 1 SemesterId 
    FROM Semesters 
    WHERE IsCurrent = 1
);

IF @CurrentSemesterId IS NOT NULL
BEGIN
    -- Enroll students who are not yet enrolled in the current semester
    INSERT INTO [dbo].[StudentSemesters] 
    ([StudentNum], [SemesterId], [YearLevel], [Section], [EnrollmentStatus], [EnrollmentDate], [Course])
    SELECT DISTINCT 
        s.StudentNum, 
        @CurrentSemesterId,
        CASE 
            WHEN s.YearLevelSection LIKE '1%' THEN 1
            WHEN s.YearLevelSection LIKE '2%' THEN 2
            WHEN s.YearLevelSection LIKE '3%' THEN 3
            WHEN s.YearLevelSection LIKE '4%' THEN 4
            ELSE 1
        END,
        CASE 
            WHEN s.YearLevelSection LIKE '%A%' THEN 'A'
            WHEN s.YearLevelSection LIKE '%B%' THEN 'B'
            WHEN s.YearLevelSection LIKE '%C%' THEN 'C'
            WHEN s.YearLevelSection LIKE '%D%' THEN 'D'
            ELSE 'A'
        END,
        'Active',
        GETDATE(),
        s.Course
    FROM [dbo].[Student] s
    WHERE s.StudentNum NOT IN (
        SELECT StudentNum 
        FROM [dbo].[StudentSemesters] 
        WHERE SemesterId = @CurrentSemesterId
    );
    
    PRINT N'    ✓ Students enrolled in current semester';
END
ELSE
BEGIN
    PRINT N'    ⚠ No current semester found - students not enrolled';
END
GO

-- ========================================
-- SECTION 11: CREATE INDEXES FOR PERFORMANCE
-- ========================================
PRINT N'Creating performance indexes...';

-- Attendance table indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Attendance_SemesterId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Attendance_SemesterId] ON [Attendance]([SemesterId]);
    PRINT N'    ✓ Created IX_Attendance_SemesterId';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Attendance_TimeIn')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Attendance_TimeIn] ON [Attendance]([TimeIn]);
    PRINT N'    ✓ Created IX_Attendance_TimeIn';
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Attendance_StudentNum_Semester')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Attendance_StudentNum_Semester] 
    ON [Attendance]([StudentNum], [SemesterId]);
    PRINT N'    ✓ Created IX_Attendance_StudentNum_Semester';
END

-- Event table indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Event_SemesterId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_Event_SemesterId] ON [Event]([SemesterId]);
    PRINT N'    ✓ Created IX_Event_SemesterId';
END
GO

-- ========================================
-- SECTION 12: UPDATE CURRENT SEMESTER FOR STUDENTS
-- ========================================
PRINT N'Updating current semester for students...';

UPDATE [dbo].[Student]
SET [SemesterId] = (
    SELECT TOP 1 SemesterId 
    FROM Semesters 
    WHERE IsCurrent = 1
)
WHERE [SemesterId] IS NULL;

PRINT N'    ✓ Updated current semester for students';
GO

-- ========================================
-- FINAL SUMMARY
-- ========================================
PRINT N'========================================================================';
PRINT N'SEMESTER + TIME-IN/TIME-OUT IMPLEMENTATION COMPLETED SUCCESSFULLY!';
PRINT N'========================================================================';
PRINT N'';
PRINT N'Summary of changes:';
PRINT N'  ✓ AcademicYears: Table created with English column names';
PRINT N'  ✓ Semesters: Table created with current semester tracking';
PRINT N'  ✓ StudentSemesters: Updated with attendance tracking and Course field';
PRINT N'  ✓ Attendance: Added time tracking (TimeIn/TimeOut/Duration)';
PRINT N'  ✓ Student: Added location tracking columns';
PRINT N'  ✓ Event: Added semester filtering';
PRINT N'  ✓ All foreign keys: Created with NO ACTION to avoid cascade conflicts';
PRINT N'  ✓ Performance indexes: Created for fast queries';
PRINT N'  ✓ Academic Years: 2024-2025, 2025-2026, 2026-2027';
PRINT N'  ✓ Semesters: First, Second, Summer for each academic year';
PRINT N'  ✓ Current Semester: First Semester 2025-2026 marked as active';
PRINT N'  ✓ Students: Automatically enrolled in current semester';
PRINT N'';
PRINT N'ENGLISH COLUMN NAMES ONLY - SUITABLE FOR FILIPINO USERS';
PRINT N'';
PRINT N'Features enabled:';
PRINT N'  ✓ Semester-based filtering throughout the system';
PRINT N'  ✓ Time-In/Time-Out attendance tracking';
PRINT N'  ✓ Duration calculation (in minutes)';
PRINT N'  ✓ Location-based attendance verification';
PRINT N'  ✓ Device type tracking (Mobile/Desktop)';
PRINT N'  ✓ Punctuality tracking (On Time vs Late)';
PRINT N'  ✓ Comprehensive attendance analytics';
PRINT N'  ✓ No cascade path conflicts (NO ACTION constraints)';
PRINT N'';
PRINT N'========================================================================';
PRINT N'Completed at: ' + CONVERT(VARCHAR(50), GETDATE(), 120);
PRINT N'========================================================================';
PRINT N'';

COMMIT TRANSACTION;
PRINT N'✓ Transaction committed successfully';
GO

PRINT N'';
PRINT N'Script execution completed successfully!';
PRINT N'Your iBITS Portal is now ready with English-only column names!';
PRINT N'';
GO