using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VetClinic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConcurrencyTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Sin cambios de esquema: la concurrencia optimista usa la columna de sistema
            // "xmin" de PostgreSQL (Property<uint>("xmin").IsRowVersion()), que ya existe
            // en todas las tablas. Esta migración documenta la incorporación del token.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
