using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using StockManager.Infrastructure.Data;

#nullable disable

namespace StockManager.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261029130000_AddFrecuenciaPagoNomina")]
    public partial class AddFrecuenciaPagoNomina : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Todo lo que ya existe era quincenal.
            migrationBuilder.AddColumn<string>(
                name: "FrecuenciaPago",
                table: "Trabajadores",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Quincenal");

            migrationBuilder.AddColumn<string>(
                name: "Frecuencia",
                table: "PeriodosNomina",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Quincenal");

            migrationBuilder.DropIndex(
                name: "IX_PeriodosNomina_FechaInicio",
                table: "PeriodosNomina");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodosNomina_Frecuencia_FechaInicio",
                table: "PeriodosNomina",
                columns: new[] { "Frecuencia", "FechaInicio" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PeriodosNomina_Frecuencia_FechaInicio",
                table: "PeriodosNomina");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodosNomina_FechaInicio",
                table: "PeriodosNomina",
                column: "FechaInicio",
                unique: true);

            migrationBuilder.DropColumn(
                name: "Frecuencia",
                table: "PeriodosNomina");

            migrationBuilder.DropColumn(
                name: "FrecuenciaPago",
                table: "Trabajadores");
        }
    }
}
