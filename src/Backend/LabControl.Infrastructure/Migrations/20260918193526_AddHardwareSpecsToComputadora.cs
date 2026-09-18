using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabControl.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHardwareSpecsToComputadora : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CpuModelo",
                table: "Computadoras",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiscoLibreGb",
                table: "Computadoras",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DiscoTotalGb",
                table: "Computadoras",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RamTotalGb",
                table: "Computadoras",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SistemaOperativo",
                table: "Computadoras",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UltimaActualizacionHardwareUtc",
                table: "Computadoras",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "UptimeHoras",
                table: "Computadoras",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CpuModelo",
                table: "Computadoras");

            migrationBuilder.DropColumn(
                name: "DiscoLibreGb",
                table: "Computadoras");

            migrationBuilder.DropColumn(
                name: "DiscoTotalGb",
                table: "Computadoras");

            migrationBuilder.DropColumn(
                name: "RamTotalGb",
                table: "Computadoras");

            migrationBuilder.DropColumn(
                name: "SistemaOperativo",
                table: "Computadoras");

            migrationBuilder.DropColumn(
                name: "UltimaActualizacionHardwareUtc",
                table: "Computadoras");

            migrationBuilder.DropColumn(
                name: "UptimeHoras",
                table: "Computadoras");
        }
    }
}
