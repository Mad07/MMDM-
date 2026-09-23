using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Mark1.Migrations
{
    /// <inheritdoc />
    public partial class AddSavingsPurposeToIncome : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SavingsPurposeId",
                table: "Incomes",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Incomes_SavingsPurposeId",
                table: "Incomes",
                column: "SavingsPurposeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Incomes_SavingsPurposes_SavingsPurposeId",
                table: "Incomes",
                column: "SavingsPurposeId",
                principalTable: "SavingsPurposes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Incomes_SavingsPurposes_SavingsPurposeId",
                table: "Incomes");

            migrationBuilder.DropIndex(
                name: "IX_Incomes_SavingsPurposeId",
                table: "Incomes");

            migrationBuilder.DropColumn(
                name: "SavingsPurposeId",
                table: "Incomes");
        }
    }
}
