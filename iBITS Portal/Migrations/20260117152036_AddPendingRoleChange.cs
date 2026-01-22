using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iBITS_Portal.Migrations
{
    /// <inheritdoc />
    public partial class AddPendingRoleChange : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PendingRoleChanges",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentNumber = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    OldRole = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NewRole = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AssignedByAdminId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    AssignedByAdminName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AssignedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    ConfirmedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeclined = table.Column<bool>(type: "bit", nullable: false),
                    DeclinedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PendingRoleChanges", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PendingRoleChanges");
        }
    }
}
