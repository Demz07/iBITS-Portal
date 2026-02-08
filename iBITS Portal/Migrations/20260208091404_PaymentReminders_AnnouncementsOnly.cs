using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iBITS_Portal.Migrations
{
    /// <inheritdoc />
    public partial class PaymentReminders_AnnouncementsOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedDate",
                table: "Announcements",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Announcements",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "RecipientCount",
                table: "Announcements",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "ViewCount",
                table: "Announcements",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DeletedDate",
                table: "Announcements");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Announcements");

            migrationBuilder.DropColumn(
                name: "RecipientCount",
                table: "Announcements");

            migrationBuilder.DropColumn(
                name: "ViewCount",
                table: "Announcements");
        }
    }
}
