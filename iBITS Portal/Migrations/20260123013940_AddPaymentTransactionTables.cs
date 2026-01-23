using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iBITS_Portal.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentTransactionTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExcuseRequest");

            // SchoolYearEnrolled and AcadYear columns already exist in database
            // Removed duplicate column additions

            migrationBuilder.CreateTable(
                name: "FinePaymentTransactions",
                columns: table => new
                {
                    FineTransactionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FineId = table.Column<int>(type: "int", nullable: false),
                    StudentNum = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProcessedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TransactionReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
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

            // Notification table already exists - skipped
            
            migrationBuilder.CreateTable(
                name: "PaymentTransactions",
                columns: table => new
                {
                    TransactionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeeId = table.Column<int>(type: "int", nullable: false),
                    StudentNum = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    PaymentDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    PaymentMethod = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ProcessedBy = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    TransactionReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    AcademicYear = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
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

            // SystemSettings table already exists - skipped
            
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

            // Notification indexes already exist - skipped
            
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinePaymentTransactions");

            migrationBuilder.DropTable(
                name: "Notification");

            migrationBuilder.DropTable(
                name: "PaymentTransactions");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropColumn(
                name: "SchoolYearEnrolled",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "AcadYear",
                table: "Fees");

            migrationBuilder.CreateTable(
                name: "ExcuseRequest",
                columns: table => new
                {
                    ExcuseRequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventId = table.Column<int>(type: "int", nullable: false),
                    StudentNum = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RequestStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ReviewDate = table.Column<DateTime>(type: "datetime", nullable: true),
                    ReviewedBy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SubmissionDate = table.Column<DateTime>(type: "datetime", nullable: false),
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

            migrationBuilder.CreateIndex(
                name: "IX_ExcuseRequest_EventId",
                table: "ExcuseRequest",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_ExcuseRequest_StudentNum",
                table: "ExcuseRequest",
                column: "StudentNum");
        }
    }
}
