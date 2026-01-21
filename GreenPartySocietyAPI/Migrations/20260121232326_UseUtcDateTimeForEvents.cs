using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreenPartySocietyAPI.Migrations
{
    /// <inheritdoc />
    public partial class UseUtcDateTimeForEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedAt",
                table: "Events",
                newName: "UpdatedAtUtc");

            migrationBuilder.RenameColumn(
                name: "StartsAt",
                table: "Events",
                newName: "StartsAtUtc");

            migrationBuilder.RenameColumn(
                name: "EndsAt",
                table: "Events",
                newName: "EndsAtUtc");

            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Events",
                newName: "CreatedAtUtc");

            migrationBuilder.RenameIndex(
                name: "IX_Events_StartsAt",
                table: "Events",
                newName: "IX_Events_StartsAtUtc");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UpdatedAtUtc",
                table: "Events",
                newName: "UpdatedAt");

            migrationBuilder.RenameColumn(
                name: "StartsAtUtc",
                table: "Events",
                newName: "StartsAt");

            migrationBuilder.RenameColumn(
                name: "EndsAtUtc",
                table: "Events",
                newName: "EndsAt");

            migrationBuilder.RenameColumn(
                name: "CreatedAtUtc",
                table: "Events",
                newName: "CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_Events_StartsAtUtc",
                table: "Events",
                newName: "IX_Events_StartsAt");
        }
    }
}
