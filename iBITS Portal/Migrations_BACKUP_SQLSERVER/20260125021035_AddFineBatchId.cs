using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iBITS_Portal.Migrations
{
    /// <inheritdoc />
    public partial class AddFineBatchId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BatchId",
                table: "Fines",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatchId",
                table: "Fines");
        }
    }
}
