using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VetClinic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase3InventoryLots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceLines_InventoryItems_InventoryItemId",
                table: "InvoiceLines");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_InventoryItems_InventoryItemId",
                table: "StockMovements");

            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "StockMovements",
                type: "numeric(12,3)",
                precision: 12,
                scale: 3,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<int>(
                name: "ProductLotId",
                table: "StockMovements",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "StockQuantity",
                table: "InventoryItems",
                type: "numeric(12,3)",
                precision: 12,
                scale: 3,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AlterColumn<decimal>(
                name: "MinStock",
                table: "InventoryItems",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<bool>(
                name: "IsFractionable",
                table: "InventoryItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "ProductLots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    InventoryItemId = table.Column<int>(type: "integer", nullable: false),
                    LotNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    ExpirationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AvailableQuantity = table.Column<decimal>(type: "numeric(12,3)", precision: 12, scale: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductLots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductLots_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_ProductLotId",
                table: "StockMovements",
                column: "ProductLotId");

            migrationBuilder.CreateIndex(
                name: "IX_ProductLots_ExpirationDate",
                table: "ProductLots",
                column: "ExpirationDate");

            migrationBuilder.CreateIndex(
                name: "IX_ProductLots_InventoryItemId",
                table: "ProductLots",
                column: "InventoryItemId");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceLines_InventoryItems_InventoryItemId",
                table: "InvoiceLines",
                column: "InventoryItemId",
                principalTable: "InventoryItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_InventoryItems_InventoryItemId",
                table: "StockMovements",
                column: "InventoryItemId",
                principalTable: "InventoryItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_ProductLots_ProductLotId",
                table: "StockMovements",
                column: "ProductLotId",
                principalTable: "ProductLots",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql("UPDATE \"StockMovements\" SET \"Type\" = 3 WHERE \"Type\" = 2;");

            migrationBuilder.Sql(@"
INSERT INTO ""ProductLots"" (""InventoryItemId"", ""LotNumber"", ""ExpirationDate"", ""AvailableQuantity"", ""IsActive"", ""CreatedAt"")
SELECT ""Id"", 'INICIAL', NULL, ""StockQuantity"", TRUE, now()
FROM ""InventoryItems""
WHERE ""StockQuantity"" > 0;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceLines_InventoryItems_InventoryItemId",
                table: "InvoiceLines");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_InventoryItems_InventoryItemId",
                table: "StockMovements");

            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_ProductLots_ProductLotId",
                table: "StockMovements");

            migrationBuilder.DropTable(
                name: "ProductLots");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_ProductLotId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "ProductLotId",
                table: "StockMovements");

            migrationBuilder.DropColumn(
                name: "IsFractionable",
                table: "InventoryItems");

            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "StockMovements",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,3)",
                oldPrecision: 12,
                oldScale: 3);

            migrationBuilder.AlterColumn<int>(
                name: "StockQuantity",
                table: "InventoryItems",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,3)",
                oldPrecision: 12,
                oldScale: 3);

            migrationBuilder.AlterColumn<int>(
                name: "MinStock",
                table: "InventoryItems",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceLines_InventoryItems_InventoryItemId",
                table: "InvoiceLines",
                column: "InventoryItemId",
                principalTable: "InventoryItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_InventoryItems_InventoryItemId",
                table: "StockMovements",
                column: "InventoryItemId",
                principalTable: "InventoryItems",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
