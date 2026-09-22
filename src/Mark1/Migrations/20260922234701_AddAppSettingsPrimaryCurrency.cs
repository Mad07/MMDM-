using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mark1.Migrations
{
    /// <inheritdoc />
    public partial class AddAppSettingsPrimaryCurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PrimaryCurrency",
                table: "AppSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PrimaryCurrency",
                table: "AppSettings");
        }
    }
}
