using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabControl.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFreezeAndVersionToComputadora : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Computadoras\" ADD COLUMN IF NOT EXISTS \"VersionClienteKiosk\" character varying(50) NULL;");
            migrationBuilder.Sql("ALTER TABLE \"Computadoras\" ADD COLUMN IF NOT EXISTS \"EstadoFreeze\" character varying(50) NOT NULL DEFAULT 'Desconocido';");
            migrationBuilder.Sql("ALTER TABLE \"Computadoras\" ADD COLUMN IF NOT EXISTS \"UltimaActualizacionExitosaUtc\" timestamp with time zone NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VersionClienteKiosk",
                table: "Computadoras");

            migrationBuilder.DropColumn(
                name: "EstadoFreeze",
                table: "Computadoras");

            migrationBuilder.DropColumn(
                name: "UltimaActualizacionExitosaUtc",
                table: "Computadoras");
        }
    }
}
