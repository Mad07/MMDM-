using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mark1.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryTransactionTypeScope : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsForExpenses",
                table: "Categories",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsForIncomes",
                table: "Categories",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsForExpenses",
                table: "Categories");

            migrationBuilder.DropColumn(
                name: "IsForIncomes",
                table: "Categories");
        }
    }
}
