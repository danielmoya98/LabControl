using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabControl.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddModoEventoToAula : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TABLE \"Computadoras\" ADD COLUMN IF NOT EXISTS \"DiscosDetalleJson\" text;");
            migrationBuilder.Sql("ALTER TABLE \"Aulas\" ADD COLUMN IF NOT EXISTS \"ModoEventoActivo\" boolean NOT NULL DEFAULT FALSE;");
            migrationBuilder.Sql("ALTER TABLE \"Aulas\" ADD COLUMN IF NOT EXISTS \"ModoEventoFinUtc\" timestamp with time zone NULL;");
            migrationBuilder.Sql("ALTER TABLE \"Aulas\" ADD COLUMN IF NOT EXISTS \"ModoEventoNombre\" character varying(100) NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiscosDetalleJson",
                table: "Computadoras");

            migrationBuilder.DropColumn(
                name: "ModoEventoActivo",
                table: "Aulas");

            migrationBuilder.DropColumn(
                name: "ModoEventoFinUtc",
                table: "Aulas");

            migrationBuilder.DropColumn(
                name: "ModoEventoNombre",
                table: "Aulas");
        }
    }
}
