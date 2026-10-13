using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace VetClinic.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase2Entities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DocumentNumber",
                table: "Tutors",
                type: "character varying(60)",
                maxLength: 60,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsSterilized",
                table: "Pets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "Pets",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TemperatureC",
                table: "MedicalRecords",
                type: "numeric(4,1)",
                precision: 4,
                scale: 1,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "WeightKg",
                table: "MedicalRecords",
                type: "numeric(6,2)",
                precision: 6,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VaccineApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PetId = table.Column<int>(type: "integer", nullable: false),
                    InventoryItemId = table.Column<int>(type: "integer", nullable: true),
                    ApplicationDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    NextDueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VaccineApplications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VaccineApplications_InventoryItems_InventoryItemId",
                        column: x => x.InventoryItemId,
                        principalTable: "InventoryItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VaccineApplications_Pets_PetId",
                        column: x => x.PetId,
                        principalTable: "Pets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tutors_DocumentNumber",
                table: "Tutors",
                column: "DocumentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VaccineApplications_InventoryItemId",
                table: "VaccineApplications",
                column: "InventoryItemId");

            migrationBuilder.CreateIndex(
                name: "IX_VaccineApplications_NextDueDate",
                table: "VaccineApplications",
                column: "NextDueDate");

            migrationBuilder.CreateIndex(
                name: "IX_VaccineApplications_PetId",
                table: "VaccineApplications",
                column: "PetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VaccineApplications");

            migrationBuilder.DropIndex(
                name: "IX_Tutors_DocumentNumber",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "DocumentNumber",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "IsSterilized",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "Pets");

            migrationBuilder.DropColumn(
                name: "TemperatureC",
                table: "MedicalRecords");

            migrationBuilder.DropColumn(
                name: "WeightKg",
                table: "MedicalRecords");
        }
    }
}
