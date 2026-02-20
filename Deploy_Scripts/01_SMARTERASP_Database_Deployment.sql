-- =============================================
-- iBITS Portal Database Deployment Script
-- Target: SmarterASP.NET SQL Server Hosting
-- Version: 1.0
-- Date: 2026-02-20
-- =============================================

-- NOTE: Before running this script:
-- 1. Create a new database in SmarterASP.NET Control Panel
-- 2. Connect to the database using myLittleAdmin or SSMS
-- 3. Execute this script in the correct order
-- 4. Verify all tables are created successfully

-- =============================================
-- SECTION 1: CREATE TABLES
-- =============================================

-- Table: Student
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Student]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Student](
        [StudentNum] [nvarchar](450) NOT NULL,
        [StudentImage] [nvarchar](max) NULL,
        [Qrcode] [nvarchar](max) NULL,
        [StudentFn] [nvarchar](max) NOT NULL,
        [StudentMn] [nvarchar](max) NULL,
        [StudentLn] [nvarchar](max) NOT NULL,
        [Birthday] [date] NULL,
        [StudentEmail] [nvarchar](max) NULL,
        [Course] [nvarchar](max) NULL,
        [YearLevelSection] [nvarchar](max) NULL,
        [StudentType] [nvarchar](max) NULL,
        [Classification] [nvarchar](max) NULL,
        [OfficerId] [int] NULL,
        [IsArchived] [bit] NULL,
        [ArchiveStatus] [nvarchar](max) NULL,
        [ArchiveDate] [date] NULL,
        [SchoolYearEnrolled] [nvarchar](max) NULL,
        CONSTRAINT [PK_Student] PRIMARY KEY CLUSTERED ([StudentNum] ASC)
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO

-- Table: Officers
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Officers]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Officers](
        [OfficerId] [int] IDENTITY(1,1) NOT NULL,
        [Classification] [nvarchar](max) NULL,
        [Position] [nvarchar](max) NULL,
        CONSTRAINT [PK_Officers] PRIMARY KEY CLUSTERED ([OfficerId] ASC)
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO

-- Table: Event
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Event]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Event](
        [EventId] [int] IDENTITY(1,1) NOT NULL,
        [EventName] [nvarchar](max) NULL,
        [EventLocation] [nvarchar](max) NULL,
        [EventDate] [date] NULL,
        [EndDate] [date] NULL,
        [StartTime] [time](7) NULL,
        [EndTime] [time](7) NULL,
        [EventDuration] [int] NULL,
        [AcadYear] [nvarchar](max) NULL,
        [EventDesc] [nvarchar](max) NULL,
        [EventType] [nvarchar](max) NULL,
        [FineForMember] [decimal](18, 2) NULL,
        [FineForClassOfficer] [decimal](18, 2) NULL,
        [FineForOrgOfficer] [decimal](18, 2) NULL,
        [NonIbitsFineForMember] [decimal](18, 2) NULL,
        [NonIbitsFineForClassOfficer] [decimal](18, 2) NULL,
        [NonIbitsFineForOrgOfficer] [decimal](18, 2) NULL,
        [IsClosed] [bit] NULL,
        CONSTRAINT [PK_Event] PRIMARY KEY CLUSTERED ([EventId] ASC)
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO

-- Table: Attendance
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Attendance]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Attendance](
        [AttendanceId] [int] IDENTITY(1,1) NOT NULL,
        [AttendanceStatus] [nvarchar](max) NULL,
        [StudentNum] [nvarchar](450) NOT NULL,
        [EventId] [int] NULL,
        CONSTRAINT [PK_Attendance] PRIMARY KEY CLUSTERED ([AttendanceId] ASC)
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO

-- Table: Remittances
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Remittances]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Remittances](
        [RemittanceId] [int] IDENTITY(1,1) NOT NULL,
        [BatchCode] [nvarchar](50) NOT NULL,
        [FeeName] [nvarchar](200) NULL,
        [FineCategory] [nvarchar](200) NULL,
        [RemittanceType] [nvarchar](50) NOT NULL,
        [Section] [nvarchar](50) NOT NULL,
        [TotalAmount] [decimal](18, 2) NOT NULL,
        [TotalStudents] [int] NOT NULL,
        [SubmittedBy] [nvarchar](450) NOT NULL,
        [SubmittedDate] [datetime2](7) NOT NULL DEFAULT (getdate()),
        [Status] [nvarchar](50) NOT NULL DEFAULT ('Pending'),
        [ValidatedBy] [nvarchar](450) NULL,
        [ValidationDate] [datetime2](7) NULL,
        [ValidationNotes] [nvarchar](max) NULL,
        [RejectionReason] [nvarchar](max) NULL,
        [AcademicYear] [nvarchar](50) NULL,
        [CreatedAt] [datetime2](7) NOT NULL DEFAULT (getdate()),
        [UpdatedAt] [datetime2](7) NULL,
        [Program] [nvarchar](100) NULL,
        CONSTRAINT [PK_Remittances] PRIMARY KEY CLUSTERED ([RemittanceId] ASC)
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO

-- Table: Fees
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Fees]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Fees](
        [FeeId] [int] IDENTITY(1,1) NOT NULL,
        [FeeName] [nvarchar](max) NULL,
        [Amount] [decimal](18, 2) NULL,
        [FeesStartDate] [date] NULL,
        [FeesDueDate] [date] NULL,
        [FeeStatus] [nvarchar](max) NULL,
        [StudentNum] [nvarchar](450) NOT NULL,
        [AcadYear] [nvarchar](max) NULL,
        [BatchId] [nvarchar](max) NULL,
        [DateCreated] [datetime] NULL,
        [AmountPaid] [decimal](18, 2) NOT NULL DEFAULT ((0.00)),
        [RemittanceStatus] [nvarchar](50) NOT NULL DEFAULT ('NotRemitted'),
        [RemittanceId] [int] NULL,
        [CollectedBy] [nvarchar](450) NULL,
        [CollectionDate] [datetime2](7) NULL,
        [OfficialPaymentDate] [datetime2](7) NULL,
        [IsPaymentLocked] [bit] NOT NULL DEFAULT ((0)),
        [PaymentLockedDate] [datetime2](7) NULL,
        [LockedBy] [nvarchar](450) NULL,
        CONSTRAINT [PK_Fees] PRIMARY KEY CLUSTERED ([FeeId] ASC)
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO

-- Table: Fines
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Fines]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Fines](
        [FineId] [int] IDENTITY(1,1) NOT NULL,
        [Amount] [decimal](18, 2) NULL,
        [FinesStartDate] [date] NULL,
        [FinesDueDate] [date] NULL,
        [FinesStatus] [nvarchar](max) NULL,
        [AttendanceId] [int] NULL,
        [Description] [nvarchar](max) NULL,
        [StudentNum] [nvarchar](450) NULL,
        [BatchId] [nvarchar](max) NULL,
        [AmountPaid] [decimal](18, 2) NOT NULL DEFAULT ((0.00)),
        [RemittanceStatus] [nvarchar](50) NOT NULL DEFAULT ('NotRemitted'),
        [RemittanceId] [int] NULL,
        [CollectedBy] [nvarchar](450) NULL,
        [CollectionDate] [datetime2](7) NULL,
        [OfficialPaymentDate] [datetime2](7) NULL,
        [IsPaymentLocked] [bit] NOT NULL DEFAULT ((0)),
        [PaymentLockedDate] [datetime2](7) NULL,
        [LockedBy] [nvarchar](450) NULL,
        CONSTRAINT [PK_Fines] PRIMARY KEY CLUSTERED ([FineId] ASC)
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO

-- Table: RemittanceItems
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[RemittanceItems]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[RemittanceItems](
        [RemittanceItemId] [int] IDENTITY(1,1) NOT NULL,
        [RemittanceId] [int] NOT NULL,
        [FeeId] [int] NULL,
        [FineId] [int] NULL,
        [StudentNum] [nvarchar](450) NOT NULL,
        [StudentName] [nvarchar](200) NULL,
        [Amount] [decimal](18, 2) NOT NULL,
        [CollectionDate] [datetime2](7) NOT NULL,
        [PaymentMethod] [nvarchar](50) NULL,
        [TransactionRef] [nvarchar](100) NULL,
        [Notes] [nvarchar](500) NULL,
        CONSTRAINT [PK_RemittanceItems] PRIMARY KEY CLUSTERED ([RemittanceItemId] ASC)
    ) ON [PRIMARY]
END
GO

-- Table: PaymentTransactions
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PaymentTransactions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PaymentTransactions](
        [TransactionId] [int] IDENTITY(1,1) NOT NULL,
        [FeeId] [int] NOT NULL,
        [StudentNum] [nvarchar](450) NOT NULL,
        [Amount] [decimal](18, 2) NOT NULL,
        [PaymentDate] [datetime2](7) NOT NULL DEFAULT (getdate()),
        [PaymentMethod] [nvarchar](50) NULL,
        [ProcessedBy] [nvarchar](450) NOT NULL,
        [TransactionReference] [nvarchar](100) NULL,
        [Notes] [nvarchar](max) NULL,
        [AcademicYear] [nvarchar](50) NULL,
        CONSTRAINT [PK_PaymentTransactions] PRIMARY KEY CLUSTERED ([TransactionId] ASC)
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO

-- Table: FinePaymentTransactions
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[FinePaymentTransactions]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[FinePaymentTransactions](
        [TransactionId] [int] IDENTITY(1,1) NOT NULL,
        [FineId] [int] NOT NULL,
        [StudentNum] [nvarchar](450) NOT NULL,
        [Amount] [decimal](18, 2) NOT NULL,
        [PaymentDate] [datetime2](7) NOT NULL DEFAULT (getdate()),
        [PaymentMethod] [nvarchar](50) NULL,
        [ProcessedBy] [nvarchar](450) NOT NULL,
        [TransactionReference] [nvarchar](100) NULL,
        [Notes] [nvarchar](max) NULL,
        CONSTRAINT [PK_FinePaymentTransactions] PRIMARY KEY CLUSTERED ([TransactionId] ASC)
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO

-- Table: Announcements
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Announcements]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Announcements](
        [AnnouncementId] [int] IDENTITY(1,1) NOT NULL,
        [Title] [nvarchar](200) NOT NULL,
        [Content] [nvarchar](max) NOT NULL,
        [CreatedBy] [nvarchar](450) NOT NULL,
        [CreatedDate] [datetime2](7) NOT NULL DEFAULT (getdate()),
        [ExpiryDate] [datetime2](7) NULL,
        [IsActive] [bit] NOT NULL DEFAULT ((1)),
        [Priority] [nvarchar](50) NULL,
        CONSTRAINT [PK_Announcements] PRIMARY KEY CLUSTERED ([AnnouncementId] ASC)
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO

-- Table: UserAnnouncementDismissals
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[UserAnnouncementDismissals]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[UserAnnouncementDismissals](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [UserId] [nvarchar](450) NOT NULL,
        [AnnouncementId] [int] NOT NULL,
        [DismissedAt] [datetime2](7) NOT NULL DEFAULT (getdate()),
        CONSTRAINT [PK_UserAnnouncementDismissals] PRIMARY KEY CLUSTERED ([Id] ASC)
    ) ON [PRIMARY]
END
GO

-- Table: Notification
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Notification]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Notification](
        [NotificationId] [int] IDENTITY(1,1) NOT NULL,
        [StudentNum] [nvarchar](450) NOT NULL,
        [Message] [nvarchar](max) NULL,
        [NotificationDate] [datetime] NULL,
        [IsRead] [bit] NULL,
        CONSTRAINT [PK_Notification] PRIMARY KEY CLUSTERED ([NotificationId] ASC)
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO

-- Table: ActivityLogs
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[ActivityLogs]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[ActivityLogs](
        [LogId] [int] IDENTITY(1,1) NOT NULL,
        [UserId] [nvarchar](450) NULL,
        [UserName] [nvarchar](256) NULL,
        [Action] [nvarchar](500) NOT NULL,
        [Timestamp] [datetime2](7) NOT NULL DEFAULT (getdate()),
        [IpAddress] [nvarchar](45) NULL,
        [Details] [nvarchar](max) NULL,
        CONSTRAINT [PK_ActivityLogs] PRIMARY KEY CLUSTERED ([LogId] ASC)
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO

-- Table: PendingRoleChanges
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[PendingRoleChanges]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[PendingRoleChanges](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [UserId] [nvarchar](450) NOT NULL,
        [RequestedRole] [nvarchar](256) NOT NULL,
        [Reason] [nvarchar](max) NULL,
        [RequestDate] [datetime2](7) NOT NULL DEFAULT (getdate()),
        [Status] [nvarchar](50) NOT NULL DEFAULT ('Pending'),
        [AdminResponse] [nvarchar](max) NULL,
        [ResponseDate] [datetime2](7) NULL,
        CONSTRAINT [PK_PendingRoleChanges] PRIMARY KEY CLUSTERED ([Id] ASC)
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO

-- Table: SystemSettings
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[SystemSettings]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[SystemSettings](
        [SettingId] [int] IDENTITY(1,1) NOT NULL,
        [SettingKey] [nvarchar](100) NOT NULL,
        [SettingValue] [nvarchar](max) NULL,
        [Description] [nvarchar](500) NULL,
        [LastModified] [datetime2](7) NOT NULL DEFAULT (getdate()),
        [ModifiedBy] [nvarchar](450) NULL,
        CONSTRAINT [PK_SystemSettings] PRIMARY KEY CLUSTERED ([SettingId] ASC)
    ) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
END
GO

-- =============================================
-- SECTION 2: CREATE ASP.NET IDENTITY TABLES
-- =============================================

-- These are created automatically by Entity Framework
-- But we'll ensure they exist for manual deployment

PRINT 'Creating ASP.NET Identity tables...'
-- (Identity tables will be created by EF migrations)

-- =============================================
-- SECTION 3: CREATE FOREIGN KEYS
-- =============================================

-- FK: Student -> Officers
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[Fk_Officer]'))
BEGIN
    ALTER TABLE [dbo].[Student] WITH CHECK ADD CONSTRAINT [Fk_Officer] 
    FOREIGN KEY([OfficerId]) REFERENCES [dbo].[Officers] ([OfficerId])
END
GO

-- FK: Attendance -> Student
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Attendance_Student]'))
BEGIN
    ALTER TABLE [dbo].[Attendance] WITH CHECK ADD CONSTRAINT [FK_Attendance_Student] 
    FOREIGN KEY([StudentNum]) REFERENCES [dbo].[Student] ([StudentNum])
END
GO

-- FK: Attendance -> Event
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Attendance_Event]'))
BEGIN
    ALTER TABLE [dbo].[Attendance] WITH CHECK ADD CONSTRAINT [FK_Attendance_Event] 
    FOREIGN KEY([EventId]) REFERENCES [dbo].[Event] ([EventId])
END
GO

-- FK: Fees -> Student
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Fees_Student]'))
BEGIN
    ALTER TABLE [dbo].[Fees] WITH CHECK ADD CONSTRAINT [FK_Fees_Student] 
    FOREIGN KEY([StudentNum]) REFERENCES [dbo].[Student] ([StudentNum])
END
GO

-- FK: Fees -> Remittances
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Fees_Remittances]'))
BEGIN
    ALTER TABLE [dbo].[Fees] WITH CHECK ADD CONSTRAINT [FK_Fees_Remittances] 
    FOREIGN KEY([RemittanceId]) REFERENCES [dbo].[Remittances] ([RemittanceId])
END
GO

-- FK: Fines -> Student
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Fines_Student]'))
BEGIN
    ALTER TABLE [dbo].[Fines] WITH CHECK ADD CONSTRAINT [FK_Fines_Student] 
    FOREIGN KEY([StudentNum]) REFERENCES [dbo].[Student] ([StudentNum])
END
GO

-- FK: Fines -> Attendance
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Fines_Attendance]'))
BEGIN
    ALTER TABLE [dbo].[Fines] WITH CHECK ADD CONSTRAINT [FK_Fines_Attendance] 
    FOREIGN KEY([AttendanceId]) REFERENCES [dbo].[Attendance] ([AttendanceId])
END
GO

-- FK: Fines -> Remittances
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Fines_Remittances]'))
BEGIN
    ALTER TABLE [dbo].[Fines] WITH CHECK ADD CONSTRAINT [FK_Fines_Remittances] 
    FOREIGN KEY([RemittanceId]) REFERENCES [dbo].[Remittances] ([RemittanceId])
END
GO

-- FK: RemittanceItems -> Remittances
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_RemittanceItems_Remittances]'))
BEGIN
    ALTER TABLE [dbo].[RemittanceItems] WITH CHECK ADD CONSTRAINT [FK_RemittanceItems_Remittances] 
    FOREIGN KEY([RemittanceId]) REFERENCES [dbo].[Remittances] ([RemittanceId])
END
GO

-- FK: RemittanceItems -> Fees
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_RemittanceItems_Fees]'))
BEGIN
    ALTER TABLE [dbo].[RemittanceItems] WITH CHECK ADD CONSTRAINT [FK_RemittanceItems_Fees] 
    FOREIGN KEY([FeeId]) REFERENCES [dbo].[Fees] ([FeeId])
END
GO

-- FK: RemittanceItems -> Fines
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_RemittanceItems_Fines]'))
BEGIN
    ALTER TABLE [dbo].[RemittanceItems] WITH CHECK ADD CONSTRAINT [FK_RemittanceItems_Fines] 
    FOREIGN KEY([FineId]) REFERENCES [dbo].[Fines] ([FineId])
END
GO

-- FK: RemittanceItems -> Student
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_RemittanceItems_Students]'))
BEGIN
    ALTER TABLE [dbo].[RemittanceItems] WITH CHECK ADD CONSTRAINT [FK_RemittanceItems_Students] 
    FOREIGN KEY([StudentNum]) REFERENCES [dbo].[Student] ([StudentNum])
END
GO

-- FK: PaymentTransactions -> Fees
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PaymentTransactions_Fees]'))
BEGIN
    ALTER TABLE [dbo].[PaymentTransactions] WITH CHECK ADD CONSTRAINT [FK_PaymentTransactions_Fees] 
    FOREIGN KEY([FeeId]) REFERENCES [dbo].[Fees] ([FeeId])
END
GO

-- FK: PaymentTransactions -> Student
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_PaymentTransactions_Student]'))
BEGIN
    ALTER TABLE [dbo].[PaymentTransactions] WITH CHECK ADD CONSTRAINT [FK_PaymentTransactions_Student] 
    FOREIGN KEY([StudentNum]) REFERENCES [dbo].[Student] ([StudentNum])
END
GO

-- FK: FinePaymentTransactions -> Fines
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_FinePaymentTransactions_Fines]'))
BEGIN
    ALTER TABLE [dbo].[FinePaymentTransactions] WITH CHECK ADD CONSTRAINT [FK_FinePaymentTransactions_Fines] 
    FOREIGN KEY([FineId]) REFERENCES [dbo].[Fines] ([FineId])
END
GO

-- FK: FinePaymentTransactions -> Student
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_FinePaymentTransactions_Student]'))
BEGIN
    ALTER TABLE [dbo].[FinePaymentTransactions] WITH CHECK ADD CONSTRAINT [FK_FinePaymentTransactions_Student] 
    FOREIGN KEY([StudentNum]) REFERENCES [dbo].[Student] ([StudentNum])
END
GO

-- FK: Notification -> Student
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE object_id = OBJECT_ID(N'[dbo].[FK_Notification_Student]'))
BEGIN
    ALTER TABLE [dbo].[Notification] WITH CHECK ADD CONSTRAINT [FK_Notification_Student] 
    FOREIGN KEY([StudentNum]) REFERENCES [dbo].[Student] ([StudentNum])
END
GO

PRINT 'Database schema created successfully!'
PRINT 'Next step: Run the data import script (02_SMARTERASP_Data_Import.sql)'
GO
