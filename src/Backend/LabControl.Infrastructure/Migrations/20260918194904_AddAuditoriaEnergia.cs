using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LabControl.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditoriaEnergia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaUltimoUsoUtc",
                table: "Computadoras",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UltimoEstudianteEmail",
                table: "Computadoras",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UltimoEstudianteNombre",
                table: "Computadoras",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RegistrosConsumoEnergia",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ComputadoraId = table.Column<int>(type: "integer", nullable: false),
                    AulaId = table.Column<int>(type: "integer", nullable: false),
                    SesionUsoId = table.Column<int>(type: "integer", nullable: true),
                    UltimoEstudianteEmail = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    UltimoEstudianteNombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FechaDeteccionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HorasInactivaEncendida = table.Column<double>(type: "double precision", nullable: false),
                    MotivoInfraccion = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Resuelto = table.Column<bool>(type: "boolean", nullable: false),
                    FechaCreacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaModificacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosConsumoEnergia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosConsumoEnergia_Aulas_AulaId",
                        column: x => x.AulaId,
                        principalTable: "Aulas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegistrosConsumoEnergia_Computadoras_ComputadoraId",
                        column: x => x.ComputadoraId,
                        principalTable: "Computadoras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegistrosConsumoEnergia_SesionesUso_SesionUsoId",
                        column: x => x.SesionUsoId,
                        principalTable: "SesionesUso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosConsumoEnergia_AulaId",
                table: "RegistrosConsumoEnergia",
                column: "AulaId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosConsumoEnergia_ComputadoraId",
                table: "RegistrosConsumoEnergia",
                column: "ComputadoraId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosConsumoEnergia_FechaDeteccionUtc",
                table: "RegistrosConsumoEnergia",
                column: "FechaDeteccionUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosConsumoEnergia_SesionUsoId",
                table: "RegistrosConsumoEnergia",
                column: "SesionUsoId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosConsumoEnergia_UltimoEstudianteEmail",
                table: "RegistrosConsumoEnergia",
                column: "UltimoEstudianteEmail");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrosConsumoEnergia");

            migrationBuilder.DropColumn(
                name: "FechaUltimoUsoUtc",
                table: "Computadoras");

            migrationBuilder.DropColumn(
                name: "UltimoEstudianteEmail",
                table: "Computadoras");

            migrationBuilder.DropColumn(
                name: "UltimoEstudianteNombre",
                table: "Computadoras");
        }
    }
}
