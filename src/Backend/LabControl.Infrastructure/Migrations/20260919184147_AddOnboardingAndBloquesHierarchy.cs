using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LabControl.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOnboardingAndBloquesHierarchy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Computadoras_Aulas_AulaId",
                table: "Computadoras");

            migrationBuilder.AddColumn<int>(
                name: "NumeroPuesto",
                table: "Computadoras",
                type: "integer",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AlterColumn<bool>(
                name: "EsRecreo",
                table: "BloquesHorarios",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "boolean");

            migrationBuilder.AlterColumn<string>(
                name: "Descripcion",
                table: "BloquesHorarios",
                type: "character varying(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DocenteId",
                table: "BloquesHorarios",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsUsoLibre",
                table: "BloquesHorarios",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "GrupoParalelo",
                table: "BloquesHorarios",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MateriaId",
                table: "BloquesHorarios",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Aulas",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(50)",
                oldMaxLength: 50);

            migrationBuilder.AddColumn<int>(
                name: "BloqueId",
                table: "Aulas",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Piso",
                table: "Aulas",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Docentes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombres = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Apellidos = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    EmailInstitucional = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    TelefonoContacto = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    FechaCreacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaModificacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Docentes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Materias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Sigla = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Carrera = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    FechaCreacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaModificacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materias", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Sedes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Codigo = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    Ciudad = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Direccion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    OnboardingCompletado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    FechaRegistroUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaCreacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaModificacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sedes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bloques",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SedeId = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Codigo = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    TienePisos = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    TotalPisos = table.Column<int>(type: "integer", nullable: true),
                    Descripcion = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    FechaCreacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    FechaModificacionUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bloques", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bloques_Sedes_SedeId",
                        column: x => x.SedeId,
                        principalTable: "Sedes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BloquesHorarios_DocenteId",
                table: "BloquesHorarios",
                column: "DocenteId");

            migrationBuilder.CreateIndex(
                name: "IX_BloquesHorarios_MateriaId",
                table: "BloquesHorarios",
                column: "MateriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Aulas_BloqueId",
                table: "Aulas",
                column: "BloqueId");

            migrationBuilder.CreateIndex(
                name: "IX_Bloques_Codigo",
                table: "Bloques",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bloques_SedeId",
                table: "Bloques",
                column: "SedeId");

            migrationBuilder.CreateIndex(
                name: "IX_Docentes_EmailInstitucional",
                table: "Docentes",
                column: "EmailInstitucional",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Materias_Sigla",
                table: "Materias",
                column: "Sigla",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sedes_Codigo",
                table: "Sedes",
                column: "Codigo",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Aulas_Bloques_BloqueId",
                table: "Aulas",
                column: "BloqueId",
                principalTable: "Bloques",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BloquesHorarios_Docentes_DocenteId",
                table: "BloquesHorarios",
                column: "DocenteId",
                principalTable: "Docentes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_BloquesHorarios_Materias_MateriaId",
                table: "BloquesHorarios",
                column: "MateriaId",
                principalTable: "Materias",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Computadoras_Aulas_AulaId",
                table: "Computadoras",
                column: "AulaId",
                principalTable: "Aulas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Aulas_Bloques_BloqueId",
                table: "Aulas");

            migrationBuilder.DropForeignKey(
                name: "FK_BloquesHorarios_Docentes_DocenteId",
                table: "BloquesHorarios");

            migrationBuilder.DropForeignKey(
                name: "FK_BloquesHorarios_Materias_MateriaId",
                table: "BloquesHorarios");

            migrationBuilder.DropForeignKey(
                name: "FK_Computadoras_Aulas_AulaId",
                table: "Computadoras");

            migrationBuilder.DropTable(
                name: "Bloques");

            migrationBuilder.DropTable(
                name: "Docentes");

            migrationBuilder.DropTable(
                name: "Materias");

            migrationBuilder.DropTable(
                name: "Sedes");

            migrationBuilder.DropIndex(
                name: "IX_BloquesHorarios_DocenteId",
                table: "BloquesHorarios");

            migrationBuilder.DropIndex(
                name: "IX_BloquesHorarios_MateriaId",
                table: "BloquesHorarios");

            migrationBuilder.DropIndex(
                name: "IX_Aulas_BloqueId",
                table: "Aulas");

            migrationBuilder.DropColumn(
                name: "NumeroPuesto",
                table: "Computadoras");

            migrationBuilder.DropColumn(
                name: "DocenteId",
                table: "BloquesHorarios");

            migrationBuilder.DropColumn(
                name: "EsUsoLibre",
                table: "BloquesHorarios");

            migrationBuilder.DropColumn(
                name: "GrupoParalelo",
                table: "BloquesHorarios");

            migrationBuilder.DropColumn(
                name: "MateriaId",
                table: "BloquesHorarios");

            migrationBuilder.DropColumn(
                name: "BloqueId",
                table: "Aulas");

            migrationBuilder.DropColumn(
                name: "Piso",
                table: "Aulas");

            migrationBuilder.AlterColumn<bool>(
                name: "EsRecreo",
                table: "BloquesHorarios",
                type: "boolean",
                nullable: false,
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "Descripcion",
                table: "BloquesHorarios",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Nombre",
                table: "Aulas",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(60)",
                oldMaxLength: 60);

            migrationBuilder.AddForeignKey(
                name: "FK_Computadoras_Aulas_AulaId",
                table: "Computadoras",
                column: "AulaId",
                principalTable: "Aulas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
