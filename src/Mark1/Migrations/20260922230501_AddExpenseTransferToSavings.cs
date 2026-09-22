using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mark1.Migrations
{
    /// <inheritdoc />
    public partial class AddExpenseTransferToSavings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTransferToSavings",
                table: "Expenses",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "TransferIncomeId",
                table: "Expenses",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTransferToSavings",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "TransferIncomeId",
                table: "Expenses");
        }
    }
}
