using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace iBITS_Portal.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Action = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    PerformedBy = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IpAddress = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Announcements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    PostedBy = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AnnouncementType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TargetAudience = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ArchivedAnnouncements",
                columns: table => new
                {
                    ArchivedAnnouncementId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AnnouncementId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    PostedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ArchivedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ArchivedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ArchiveReason = table.Column<string>(type: "text", nullable: false),
                    ArchiveNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchivedAnnouncements", x => x.ArchivedAnnouncementId);
                });

            migrationBuilder.CreateTable(
                name: "ArchivedEvents",
                columns: table => new
                {
                    ArchivedEventId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventId = table.Column<int>(type: "integer", nullable: false),
                    EventName = table.Column<string>(type: "text", nullable: false),
                    EventLocation = table.Column<string>(type: "text", nullable: true),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: true),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    EventDuration = table.Column<string>(type: "text", nullable: true),
                    AcadYear = table.Column<string>(type: "text", nullable: true),
                    EventDesc = table.Column<string>(type: "text", nullable: true),
                    EventType = table.Column<string>(type: "text", nullable: true),
                    FineForMember = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    FineForClassOfficer = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    FineForOrgOfficer = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    NonIbitsFineForMember = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    NonIbitsFineForClassOfficer = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    NonIbitsFineForOrgOfficer = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    ArchivedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ArchivedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    ArchiveReason = table.Column<string>(type: "text", nullable: true),
                    ArchiveNotes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AttendanceRecordsAffected = table.Column<int>(type: "integer", nullable: false),
                    TotalFinesAffected = table.Column<decimal>(type: "numeric(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchivedEvents", x => x.ArchivedEventId);
                });

            migrationBuilder.CreateTable(
                name: "Event",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EventName = table.Column<string>(type: "text", nullable: true),
                    EventLocation = table.Column<string>(type: "text", nullable: true),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    EventDuration = table.Column<string>(type: "text", nullable: true),
                    AcadYear = table.Column<string>(type: "text", nullable: true),
                    EventDesc = table.Column<string>(type: "text", nullable: true),
                    EventType = table.Column<string>(type: "text", nullable: true),
                    FineForMember = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    FineForClassOfficer = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    FineForOrgOfficer = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    NonIbitsFineForMember = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    NonIbitsFineForClassOfficer = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    NonIbitsFineForOrgOfficer = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    IsClosed = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Event", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "Officers",
                columns: table => new
                {
                    OfficerId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Classification = table.Column<string>(type: "text", nullable: true),
                    Position = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Officers", x => x.OfficerId);
                });

            migrationBuilder.CreateTable(
                name: "PendingRoleChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentNumber = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    OldRole = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    NewRole = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AssignedByAdminId = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    AssignedByAdminName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    AssignedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IsConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    ConfirmedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsDeclined = table.Column<bool>(type: "boolean", nullable: false),
                    DeclinedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingRoleChanges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SettingKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SettingValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Student",
                columns: table => new
                {
                    StudentNum = table.Column<string>(type: "text", nullable: false),
                    StudentImage = table.Column<string>(type: "text", nullable: true),
                    Qrcode = table.Column<string>(type: "text", nullable: true),
                    StudentFn = table.Column<string>(type: "text", nullable: false),
                    StudentMn = table.Column<string>(type: "text", nullable: true),
                    StudentLn = table.Column<string>(type: "text", nullable: false),
                    Birthday = table.Column<DateOnly>(type: "date", nullable: true),
                    StudentEmail = table.Column<string>(type: "text", nullable: true),
                    Course = table.Column<string>(type: "text", nullable: true),
                    YearLevelSection = table.Column<string>(type: "text", nullable: true),
                    StudentType = table.Column<string>(type: "text", nullable: true),
                    Classification = table.Column<string>(type: "text", nullable: true),
                    OfficerId = table.Column<int>(type: "integer", nullable: true),
                    IsArchived = table.Column<bool>(type: "boolean", nullable: true),
                    ArchiveStatus = table.Column<string>(type: "text", nullable: true),
                    ArchiveDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SchoolYearEnrolled = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Student", x => x.StudentNum);
                    table.ForeignKey(
                        name: "Fk_Officer",
                        column: x => x.OfficerId,
                        principalTable: "Officers",
                        principalColumn: "OfficerId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Attendance",
                columns: table => new
                {
                    AttendanceId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AttendanceStatus = table.Column<string>(type: "text", nullable: true),
                    StudentNum = table.Column<string>(type: "text", nullable: false),
                    EventId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendance", x => x.AttendanceId);
                    table.ForeignKey(
                        name: "FK_Attendance_Event",
                        column: x => x.EventId,
                        principalTable: "Event",
                        principalColumn: "EventId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Attendance_Student",
                        column: x => x.StudentNum,
                        principalTable: "Student",
                        principalColumn: "StudentNum",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notification",
                columns: table => new
                {
                    NotificationId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentNum = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NotificationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    NotificationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    SentBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notification", x => x.NotificationId);
                    table.ForeignKey(
                        name: "FK_Notification_Student",
                        column: x => x.StudentNum,
                        principalTable: "Student",
                        principalColumn: "StudentNum",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Remittances",
                columns: table => new
                {
                    RemittanceId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BatchCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FeeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FineCategory = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RemittanceType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Program = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Section = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    TotalStudents = table.Column<int>(type: "integer", nullable: false),
                    SubmittedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "GETDATE()"),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    ValidatedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    ValidationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ValidationNotes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    AcademicYear = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Remittances", x => x.RemittanceId);
                    table.ForeignKey(
                        name: "FK_Remittances_Student_SubmittedBy",
                        column: x => x.SubmittedBy,
                        principalTable: "Student",
                        principalColumn: "StudentNum");
                    table.ForeignKey(
                        name: "FK_Remittances_Student_ValidatedBy",
                        column: x => x.ValidatedBy,
                        principalTable: "Student",
                        principalColumn: "StudentNum");
                });

            migrationBuilder.CreateTable(
                name: "UserAnnouncementDismissals",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    StudentNum = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    AnnouncementId = table.Column<int>(type: "integer", nullable: false),
                    DismissedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAnnouncementDismissals", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserAnnouncementDismissal_Announcement",
                        column: x => x.AnnouncementId,
                        principalTable: "Announcements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserAnnouncementDismissal_Student",
                        column: x => x.StudentNum,
                        principalTable: "Student",
                        principalColumn: "StudentNum",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Fees",
                columns: table => new
                {
                    FeeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FeeName = table.Column<string>(type: "text", nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    AmountPaid = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    FeesStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    FeesDueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    FeeStatus = table.Column<string>(type: "text", nullable: true),
                    AcadYear = table.Column<string>(type: "text", nullable: true),
                    BatchId = table.Column<string>(type: "text", nullable: true),
                    DateCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    StudentNum = table.Column<string>(type: "text", nullable: false),
                    RemittanceStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "NotRemitted"),
                    RemittanceId = table.Column<int>(type: "integer", nullable: true),
                    CollectedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    CollectionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OfficialPaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsPaymentLocked = table.Column<bool>(type: "boolean", nullable: false),
                    PaymentLockedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LockedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fees", x => x.FeeId);
                    table.ForeignKey(
                        name: "FK_Fees_Remittances_RemittanceId",
                        column: x => x.RemittanceId,
                        principalTable: "Remittances",
                        principalColumn: "RemittanceId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Fees_Student",
                        column: x => x.StudentNum,
                        principalTable: "Student",
                        principalColumn: "StudentNum",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Fees_Student_CollectedBy",
                        column: x => x.CollectedBy,
                        principalTable: "Student",
                        principalColumn: "StudentNum");
                    table.ForeignKey(
                        name: "FK_Fees_Student_LockedBy",
                        column: x => x.LockedBy,
                        principalTable: "Student",
                        principalColumn: "StudentNum");
                });

            migrationBuilder.CreateTable(
                name: "Fines",
                columns: table => new
                {
                    FineId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: true),
                    AmountPaid = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    FinesStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    FinesDueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    FinesStatus = table.Column<string>(type: "text", nullable: true),
                    AttendanceId = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    StudentNum = table.Column<string>(type: "text", nullable: true),
                    BatchId = table.Column<string>(type: "text", nullable: true),
                    RemittanceStatus = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "NotRemitted"),
                    RemittanceId = table.Column<int>(type: "integer", nullable: true),
                    CollectedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true),
                    CollectionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    OfficialPaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    IsPaymentLocked = table.Column<bool>(type: "boolean", nullable: false),
                    PaymentLockedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LockedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fines", x => x.FineId);
                    table.ForeignKey(
                        name: "FK_Fines_Attendance",
                        column: x => x.AttendanceId,
                        principalTable: "Attendance",
                        principalColumn: "AttendanceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Fines_Remittances_RemittanceId",
                        column: x => x.RemittanceId,
                        principalTable: "Remittances",
                        principalColumn: "RemittanceId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Fines_Student",
                        column: x => x.StudentNum,
                        principalTable: "Student",
                        principalColumn: "StudentNum");
                    table.ForeignKey(
                        name: "FK_Fines_Student_CollectedBy",
                        column: x => x.CollectedBy,
                        principalTable: "Student",
                        principalColumn: "StudentNum");
                    table.ForeignKey(
                        name: "FK_Fines_Student_LockedBy",
                        column: x => x.LockedBy,
                        principalTable: "Student",
                        principalColumn: "StudentNum");
                });

            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    TransactionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FeeId = table.Column<int>(type: "integer", nullable: false),
                    StudentNum = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "GETDATE()"),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProcessedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    TransactionReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AcademicYear = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentTransactions", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Fees_FeeId",
                        column: x => x.FeeId,
                        principalTable: "Fees",
                        principalColumn: "FeeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Student_ProcessedBy",
                        column: x => x.ProcessedBy,
                        principalTable: "Student",
                        principalColumn: "StudentNum");
                    table.ForeignKey(
                        name: "FK_PaymentTransactions_Student_StudentNum",
                        column: x => x.StudentNum,
                        principalTable: "Student",
                        principalColumn: "StudentNum");
                });

            migrationBuilder.CreateTable(
                name: "FinePaymentTransactions",
                columns: table => new
                {
                    FineTransactionId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FineId = table.Column<int>(type: "integer", nullable: false),
                    StudentNum = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(10,2)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "GETDATE()"),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    ProcessedBy = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    TransactionReference = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinePaymentTransactions", x => x.FineTransactionId);
                    table.ForeignKey(
                        name: "FK_FinePaymentTransactions_Fines_FineId",
                        column: x => x.FineId,
                        principalTable: "Fines",
                        principalColumn: "FineId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinePaymentTransactions_Student_ProcessedBy",
                        column: x => x.ProcessedBy,
                        principalTable: "Student",
                        principalColumn: "StudentNum");
                    table.ForeignKey(
                        name: "FK_FinePaymentTransactions_Student_StudentNum",
                        column: x => x.StudentNum,
                        principalTable: "Student",
                        principalColumn: "StudentNum");
                });

            migrationBuilder.CreateTable(
                name: "RemittanceItems",
                columns: table => new
                {
                    RemittanceItemId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RemittanceId = table.Column<int>(type: "integer", nullable: false),
                    FeeId = table.Column<int>(type: "integer", nullable: true),
                    FineId = table.Column<int>(type: "integer", nullable: true),
                    StudentNum = table.Column<string>(type: "character varying(450)", maxLength: 450, nullable: false),
                    StudentName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", nullable: false),
                    CollectionDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PaymentMethod = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TransactionRef = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemittanceItems", x => x.RemittanceItemId);
                    table.ForeignKey(
                        name: "FK_RemittanceItems_Fees_FeeId",
                        column: x => x.FeeId,
                        principalTable: "Fees",
                        principalColumn: "FeeId");
                    table.ForeignKey(
                        name: "FK_RemittanceItems_Fines_FineId",
                        column: x => x.FineId,
                        principalTable: "Fines",
                        principalColumn: "FineId");
                    table.ForeignKey(
                        name: "FK_RemittanceItems_Remittances_RemittanceId",
                        column: x => x.RemittanceId,
                        principalTable: "Remittances",
                        principalColumn: "RemittanceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RemittanceItems_Student_StudentNum",
                        column: x => x.StudentNum,
                        principalTable: "Student",
                        principalColumn: "StudentNum");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_EventId",
                table: "Attendance",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_StudentNum",
                table: "Attendance",
                column: "StudentNum");

            migrationBuilder.CreateIndex(
                name: "IX_Fees_CollectedBy",
                table: "Fees",
                column: "CollectedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Fees_LockedBy",
                table: "Fees",
                column: "LockedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Fees_RemittanceId",
                table: "Fees",
                column: "RemittanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Fees_RemittanceStatus",
                table: "Fees",
                column: "RemittanceStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Fees_StudentNum",
                table: "Fees",
                column: "StudentNum");

            migrationBuilder.CreateIndex(
                name: "IX_FinePaymentTransactions_FineId",
                table: "FinePaymentTransactions",
                column: "FineId");

            migrationBuilder.CreateIndex(
                name: "IX_FinePaymentTransactions_PaymentDate",
                table: "FinePaymentTransactions",
                column: "PaymentDate");

            migrationBuilder.CreateIndex(
                name: "IX_FinePaymentTransactions_ProcessedBy",
                table: "FinePaymentTransactions",
                column: "ProcessedBy");

            migrationBuilder.CreateIndex(
                name: "IX_FinePaymentTransactions_StudentNum",
                table: "FinePaymentTransactions",
                column: "StudentNum");

            migrationBuilder.CreateIndex(
                name: "IX_Fines_AttendanceId",
                table: "Fines",
                column: "AttendanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Fines_CollectedBy",
                table: "Fines",
                column: "CollectedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Fines_LockedBy",
                table: "Fines",
                column: "LockedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Fines_RemittanceId",
                table: "Fines",
                column: "RemittanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Fines_RemittanceStatus",
                table: "Fines",
                column: "RemittanceStatus");

            migrationBuilder.CreateIndex(
                name: "IX_Fines_StudentNum",
                table: "Fines",
                column: "StudentNum");

            migrationBuilder.CreateIndex(
                name: "IX_Notification_StudentNum",
                table: "Notification",
                column: "StudentNum");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_FeeId",
                table: "PaymentTransactions",
                column: "FeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_PaymentDate",
                table: "PaymentTransactions",
                column: "PaymentDate");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_ProcessedBy",
                table: "PaymentTransactions",
                column: "ProcessedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentTransactions_StudentNum",
                table: "PaymentTransactions",
                column: "StudentNum");

            migrationBuilder.CreateIndex(
                name: "IX_RemittanceItems_FeeId",
                table: "RemittanceItems",
                column: "FeeId");

            migrationBuilder.CreateIndex(
                name: "IX_RemittanceItems_FineId",
                table: "RemittanceItems",
                column: "FineId");

            migrationBuilder.CreateIndex(
                name: "IX_RemittanceItems_RemittanceId",
                table: "RemittanceItems",
                column: "RemittanceId");

            migrationBuilder.CreateIndex(
                name: "IX_RemittanceItems_StudentNum",
                table: "RemittanceItems",
                column: "StudentNum");

            migrationBuilder.CreateIndex(
                name: "IX_Remittances_BatchCode",
                table: "Remittances",
                column: "BatchCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Remittances_Section",
                table: "Remittances",
                column: "Section");

            migrationBuilder.CreateIndex(
                name: "IX_Remittances_Status",
                table: "Remittances",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Remittances_SubmittedBy",
                table: "Remittances",
                column: "SubmittedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Remittances_ValidatedBy",
                table: "Remittances",
                column: "ValidatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_Student_OfficerId",
                table: "Student",
                column: "OfficerId");

            migrationBuilder.CreateIndex(
                name: "IX_UserAnnouncementDismissals_AnnouncementId",
                table: "UserAnnouncementDismissals",
                column: "AnnouncementId");

            migrationBuilder.CreateIndex(
                name: "UQ_StudentAnnouncement",
                table: "UserAnnouncementDismissals",
                columns: new[] { "StudentNum", "AnnouncementId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropTable(
                name: "ArchivedAnnouncements");

            migrationBuilder.DropTable(
                name: "ArchivedEvents");

            migrationBuilder.DropTable(
                name: "FinePaymentTransactions");

            migrationBuilder.DropTable(
                name: "Notification");

            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "PendingRoleChanges");

            migrationBuilder.DropTable(
                name: "RemittanceItems");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropTable(
                name: "UserAnnouncementDismissals");

            migrationBuilder.DropTable(
                name: "Fees");

            migrationBuilder.DropTable(
                name: "Fines");

            migrationBuilder.DropTable(
                name: "Announcements");

            migrationBuilder.DropTable(
                name: "Attendance");

            migrationBuilder.DropTable(
                name: "Remittances");

            migrationBuilder.DropTable(
                name: "Event");

            migrationBuilder.DropTable(
                name: "Student");

            migrationBuilder.DropTable(
                name: "Officers");
        }
    }
}
