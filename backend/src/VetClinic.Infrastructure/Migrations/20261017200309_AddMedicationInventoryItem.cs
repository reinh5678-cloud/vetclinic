using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetClinic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicationInventoryItem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InventoryItemId",
                table: "MedicationAdministrations",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MedicationAdministrations_InventoryItemId",
                table: "MedicationAdministrations",
                column: "InventoryItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicationAdministrations_InventoryItems_InventoryItemId",
                table: "MedicationAdministrations",
                column: "InventoryItemId",
                principalTable: "InventoryItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicationAdministrations_InventoryItems_InventoryItemId",
                table: "MedicationAdministrations");

            migrationBuilder.DropIndex(
                name: "IX_MedicationAdministrations_InventoryItemId",
                table: "MedicationAdministrations");

            migrationBuilder.DropColumn(
                name: "InventoryItemId",
                table: "MedicationAdministrations");
        }
    }
}
