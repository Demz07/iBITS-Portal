using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iBITS_Portal.Migrations
{
    /// <inheritdoc />
    public partial class UpdateNavigationProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add columns only if they don't exist
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Fees]') AND name = 'BatchId')
                BEGIN
                    ALTER TABLE [Fees] ADD [BatchId] nvarchar(max) NULL
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Fees]') AND name = 'DateCreated')
                BEGIN
                    ALTER TABLE [Fees] ADD [DateCreated] datetime2 NULL
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Announcements]') AND name = 'AnnouncementType')
                BEGIN
                    ALTER TABLE [Announcements] ADD [AnnouncementType] nvarchar(50) NULL
                END
            ");

            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[dbo].[Announcements]') AND name = 'TargetAudience')
                BEGIN
                    ALTER TABLE [Announcements] ADD [TargetAudience] nvarchar(100) NULL
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BatchId",
                table: "Fees");

            migrationBuilder.DropColumn(
                name: "DateCreated",
                table: "Fees");

            migrationBuilder.DropColumn(
                name: "AnnouncementType",
                table: "Announcements");

            migrationBuilder.DropColumn(
                name: "TargetAudience",
                table: "Announcements");
        }
    }
}
