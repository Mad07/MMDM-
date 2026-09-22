using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mark1.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseTransferToRetained : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTransferToRetained",
                table: "Expenses",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTransferToRetained",
                table: "Expenses");
        }
    }
}
