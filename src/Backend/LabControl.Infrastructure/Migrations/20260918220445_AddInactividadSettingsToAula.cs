using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LabControl.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInactividadSettingsToAula : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AccionInactividad",
                table: "Aulas",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MinutosInactividadMaximo",
                table: "Aulas",
                type: "integer",
                nullable: false,
                defaultValue: 15);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccionInactividad",
                table: "Aulas");

            migrationBuilder.DropColumn(
                name: "MinutosInactividadMaximo",
                table: "Aulas");
        }
    }
}
