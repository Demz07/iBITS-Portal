using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iBITS_Portal.Migrations
{
    /// <inheritdoc />
    public partial class AddManualFines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Fines",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StudentNum",
                table: "Fines",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Fines_StudentNum",
                table: "Fines",
                column: "StudentNum");

            migrationBuilder.AddForeignKey(
                name: "FK_Fines_Student",
                table: "Fines",
                column: "StudentNum",
                principalTable: "Student",
                principalColumn: "StudentNum");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Fines_Student",
                table: "Fines");

            migrationBuilder.DropIndex(
                name: "IX_Fines_StudentNum",
                table: "Fines");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Fines");

            migrationBuilder.DropColumn(
                name: "StudentNum",
                table: "Fines");
        }
    }
}
