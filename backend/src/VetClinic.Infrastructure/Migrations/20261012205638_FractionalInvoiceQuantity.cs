using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetClinic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FractionalInvoiceQuantity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "Quantity",
                table: "InvoiceLines",
                type: "numeric(12,3)",
                precision: 12,
                scale: 3,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Quantity",
                table: "InvoiceLines",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,3)",
                oldPrecision: 12,
                oldScale: 3);
        }
    }
}
