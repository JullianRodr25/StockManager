using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using StockManager.Infrastructure.Data;

#nullable disable

namespace StockManager.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261028010000_AddPagoNominaEnCaja")]
    public partial class AddPagoNominaEnCaja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalPagosNominaEfectivo",
                table: "TurnosCaja",
                type: "decimal(12, 2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TurnoCajaId",
                table: "LiquidacionesNomina",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_LiquidacionesNomina_TurnoCajaId",
                table: "LiquidacionesNomina",
                column: "TurnoCajaId");

            migrationBuilder.AddForeignKey(
                name: "FK_LiquidacionesNomina_TurnosCaja_TurnoCajaId",
                table: "LiquidacionesNomina",
                column: "TurnoCajaId",
                principalTable: "TurnosCaja",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_LiquidacionesNomina_TurnosCaja_TurnoCajaId",
                table: "LiquidacionesNomina");

            migrationBuilder.DropIndex(
                name: "IX_LiquidacionesNomina_TurnoCajaId",
                table: "LiquidacionesNomina");

            migrationBuilder.DropColumn(
                name: "TurnoCajaId",
                table: "LiquidacionesNomina");

            migrationBuilder.DropColumn(
                name: "TotalPagosNominaEfectivo",
                table: "TurnosCaja");
        }
    }
}
