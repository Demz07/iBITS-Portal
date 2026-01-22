using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iBITS_Portal.Migrations
{
    /// <inheritdoc />
    public partial class Initial_PortaliBits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PerformedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Announcements",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PostedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Announcements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ArchivedAnnouncements",
                columns: table => new
                {
                    ArchivedAnnouncementId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnnouncementId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PostedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArchivedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArchivedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ArchiveReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ArchiveNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchivedAnnouncements", x => x.ArchivedAnnouncementId);
                });

            migrationBuilder.CreateTable(
                name: "ArchivedEvents",
                columns: table => new
                {
                    ArchivedEventId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    EventName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventLocation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: true),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    EventDuration = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AcadYear = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventDesc = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FineForMember = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FineForClassOfficer = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FineForOrgOfficer = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NonIbitsFineForMember = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NonIbitsFineForClassOfficer = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NonIbitsFineForOrgOfficer = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ArchivedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArchivedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ArchiveReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ArchiveNotes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AttendanceRecordsAffected = table.Column<int>(type: "int", nullable: false),
                    TotalFinesAffected = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArchivedEvents", x => x.ArchivedEventId);
                });

            migrationBuilder.CreateTable(
                name: "Event",
                columns: table => new
                {
                    EventId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventLocation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: true),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    EventDuration = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AcadYear = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventDesc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FineForMember = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FineForClassOfficer = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FineForOrgOfficer = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NonIbitsFineForMember = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NonIbitsFineForClassOfficer = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NonIbitsFineForOrgOfficer = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Event", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "Officers",
                columns: table => new
                {
                    OfficerId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Classification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Position = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Officers", x => x.OfficerId);
                });

            migrationBuilder.CreateTable(
                name: "Student",
                columns: table => new
                {
                    StudentNum = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StudentImage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Qrcode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StudentFn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StudentMn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StudentLn = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Birthday = table.Column<DateOnly>(type: "date", nullable: true),
                    StudentEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Course = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    YearLevelSection = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StudentType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Classification = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OfficerId = table.Column<int>(type: "int", nullable: true),
                    IsArchived = table.Column<bool>(type: "bit", nullable: true),
                    ArchiveStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ArchiveDate = table.Column<DateOnly>(type: "date", nullable: true)
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
                    AttendanceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AttendanceStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StudentNum = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EventId = table.Column<int>(type: "int", nullable: true)
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
                name: "ExcuseRequest",
                columns: table => new
                {
                    ExcuseRequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentNum = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    RequestStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SubmissionDate = table.Column<DateTime>(type: "datetime", nullable: false),
                    ReviewDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ReviewedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SubmissionType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExcuseRequest", x => x.ExcuseRequestId);
                    table.ForeignKey(
                        name: "FK_ExcuseRequest_Event",
                        column: x => x.EventId,
                        principalTable: "Event",
                        principalColumn: "EventId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ExcuseRequest_Student",
                        column: x => x.StudentNum,
                        principalTable: "Student",
                        principalColumn: "StudentNum",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Fees",
                columns: table => new
                {
                    FeeId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeeName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FeesStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    FeesDueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    FeeStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StudentNum = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fees", x => x.FeeId);
                    table.ForeignKey(
                        name: "FK_Fees_Student",
                        column: x => x.StudentNum,
                        principalTable: "Student",
                        principalColumn: "StudentNum",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Fines",
                columns: table => new
                {
                    FineId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    FinesStartDate = table.Column<DateOnly>(type: "date", nullable: true),
                    FinesDueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    FinesStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AttendanceId = table.Column<int>(type: "int", nullable: true)
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
                name: "IX_ExcuseRequest_EventId",
                table: "ExcuseRequest",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_ExcuseRequest_StudentNum",
                table: "ExcuseRequest",
                column: "StudentNum");

            migrationBuilder.CreateIndex(
                name: "IX_Fees_StudentNum",
                table: "Fees",
                column: "StudentNum");

            migrationBuilder.CreateIndex(
                name: "IX_Fines_AttendanceId",
                table: "Fines",
                column: "AttendanceId");

            migrationBuilder.CreateIndex(
                name: "IX_Student_OfficerId",
                table: "Student",
                column: "OfficerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropTable(
                name: "Announcements");

            migrationBuilder.DropTable(
                name: "ArchivedAnnouncements");

            migrationBuilder.DropTable(
                name: "ArchivedEvents");

            migrationBuilder.DropTable(
                name: "ExcuseRequest");

            migrationBuilder.DropTable(
                name: "Fees");

            migrationBuilder.DropTable(
                name: "Fines");

            migrationBuilder.DropTable(
                name: "Attendance");

            migrationBuilder.DropTable(
                name: "Event");

            migrationBuilder.DropTable(
                name: "Student");

            migrationBuilder.DropTable(
                name: "Officers");
        }
    }
}
