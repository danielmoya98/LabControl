using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LabControl.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPeriodosAcademicosAndSedeSegmentoRed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SegmentoRed",
                table: "Sedes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocenteEmailManual",
                table: "BloquesHorarios",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DocenteNombreManual",
                table: "BloquesHorarios",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MateriaNombreManual",
                table: "BloquesHorarios",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PeriodoAcademicoId",
                table: "BloquesHorarios",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PeriodosAcademicos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "date", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "date", nullable: false),
                    EsActual = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    FechaRegistroUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaCreacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaModificacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodosAcademicos", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BloquesHorarios_PeriodoAcademicoId",
                table: "BloquesHorarios",
                column: "PeriodoAcademicoId");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodosAcademicos_EsActual",
                table: "PeriodosAcademicos",
                column: "EsActual");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodosAcademicos_Nombre",
                table: "PeriodosAcademicos",
                column: "Nombre",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BloquesHorarios_PeriodosAcademicos_PeriodoAcademicoId",
                table: "BloquesHorarios",
                column: "PeriodoAcademicoId",
                principalTable: "PeriodosAcademicos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BloquesHorarios_PeriodosAcademicos_PeriodoAcademicoId",
                table: "BloquesHorarios");

            migrationBuilder.DropTable(
                name: "PeriodosAcademicos");

            migrationBuilder.DropIndex(
                name: "IX_BloquesHorarios_PeriodoAcademicoId",
                table: "BloquesHorarios");

            migrationBuilder.DropColumn(
                name: "SegmentoRed",
                table: "Sedes");

            migrationBuilder.DropColumn(
                name: "DocenteEmailManual",
                table: "BloquesHorarios");

            migrationBuilder.DropColumn(
                name: "DocenteNombreManual",
                table: "BloquesHorarios");

            migrationBuilder.DropColumn(
                name: "MateriaNombreManual",
                table: "BloquesHorarios");

            migrationBuilder.DropColumn(
                name: "PeriodoAcademicoId",
                table: "BloquesHorarios");
        }
    }
}
