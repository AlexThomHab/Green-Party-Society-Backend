using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GreenPartySocietyAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddSubstackUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SubstackUrl",
                table: "Users",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubstackUrl",
                table: "Users");
        }
    }
}
