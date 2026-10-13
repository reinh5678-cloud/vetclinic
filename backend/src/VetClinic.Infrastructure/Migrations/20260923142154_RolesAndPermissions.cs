using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VetClinic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RolesAndPermissions : Migration
    {
        private const string AdminPermissions = "[\"patients.view\",\"patients.manage\",\"appointments.view\",\"appointments.manage\",\"medical.view\",\"medical.manage\",\"hospitalization.view\",\"hospitalization.manage\",\"inventory.view\",\"inventory.manage\",\"invoices.view\",\"invoices.manage\",\"reports.view\"]";
        private const string VetPermissions = "[\"patients.view\",\"patients.manage\",\"appointments.view\",\"appointments.manage\",\"medical.view\",\"medical.manage\",\"hospitalization.view\",\"hospitalization.manage\",\"inventory.view\",\"inventory.manage\",\"invoices.view\",\"invoices.manage\"]";
        private const string ReceptionPermissions = "[\"patients.view\",\"patients.manage\",\"appointments.view\",\"appointments.manage\",\"invoices.view\",\"invoices.manage\"]";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PermissionsJson = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Name", "PermissionsJson", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { "Administrador", AdminPermissions, new DateTime(2026, 9, 23, 0, 0, 0, DateTimeKind.Utc), null },
                    { "Veterinario", VetPermissions, new DateTime(2026, 9, 23, 0, 0, 0, DateTimeKind.Utc), null },
                    { "Recepcionista", ReceptionPermissions, new DateTime(2026, 9, 23, 0, 0, 0, DateTimeKind.Utc), null }
                });

            migrationBuilder.AddColumn<int>(
                name: "RoleId",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PinAccess",
                table: "Users",
                type: "text",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"Users\" SET \"RoleId\" = (SELECT \"Id\" FROM \"Roles\" WHERE \"Name\" = 'Veterinario') WHERE \"Role\" = 0;");
            migrationBuilder.Sql(
                "UPDATE \"Users\" SET \"RoleId\" = (SELECT \"Id\" FROM \"Roles\" WHERE \"Name\" = 'Recepcionista') WHERE \"Role\" = 1;");
            migrationBuilder.Sql(
                "UPDATE \"Users\" SET \"RoleId\" = (SELECT \"Id\" FROM \"Roles\" WHERE \"Name\" = 'Administrador') WHERE \"Role\" = 2;");

            migrationBuilder.AlterColumn<int>(
                name: "RoleId",
                table: "Users",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_PinAccess",
                table: "Users",
                column: "PinAccess",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_RoleId",
                table: "Users",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Name",
                table: "Roles",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropColumn(
                name: "Role",
                table: "Users");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "Users",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql(
                "UPDATE \"Users\" u SET \"Role\" = CASE r.\"Name\" " +
                "WHEN 'Veterinario' THEN 0 WHEN 'Recepcionista' THEN 1 WHEN 'Administrador' THEN 2 ELSE 0 END " +
                "FROM \"Roles\" r WHERE u.\"RoleId\" = r.\"Id\";");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_PinAccess",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_RoleId",
                table: "Users");

            migrationBuilder.DropTable(
                name: "Roles");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "PinAccess",
                table: "Users");
        }
    }
}