using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Mark1.Migrations
{
    /// <inheritdoc />
    public partial class AddSavingsPurpose : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SavingsPurposeId",
                table: "Expenses",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SavingsPurposes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SavingsPurposes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SavingsPurposes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_SavingsPurposeId",
                table: "Expenses",
                column: "SavingsPurposeId");

            migrationBuilder.CreateIndex(
                name: "IX_SavingsPurposes_UserId",
                table: "SavingsPurposes",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Expenses_SavingsPurposes_SavingsPurposeId",
                table: "Expenses",
                column: "SavingsPurposeId",
                principalTable: "SavingsPurposes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Expenses_SavingsPurposes_SavingsPurposeId",
                table: "Expenses");

            migrationBuilder.DropTable(
                name: "SavingsPurposes");

            migrationBuilder.DropIndex(
                name: "IX_Expenses_SavingsPurposeId",
                table: "Expenses");

            migrationBuilder.DropColumn(
                name: "SavingsPurposeId",
                table: "Expenses");
        }
    }
}
