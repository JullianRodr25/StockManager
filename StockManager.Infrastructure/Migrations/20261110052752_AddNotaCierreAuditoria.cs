using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotaCierreAuditoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaNotaActualizada",
                table: "TurnosCaja",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NotaActualizadaPorEmpleadoId",
                table: "TurnosCaja",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TurnosCaja_NotaActualizadaPorEmpleadoId",
                table: "TurnosCaja",
                column: "NotaActualizadaPorEmpleadoId");

            migrationBuilder.AddForeignKey(
                name: "FK_TurnosCaja_Empleados_NotaActualizadaPorEmpleadoId",
                table: "TurnosCaja",
                column: "NotaActualizadaPorEmpleadoId",
                principalTable: "Empleados",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TurnosCaja_Empleados_NotaActualizadaPorEmpleadoId",
                table: "TurnosCaja");

            migrationBuilder.DropIndex(
                name: "IX_TurnosCaja_NotaActualizadaPorEmpleadoId",
                table: "TurnosCaja");

            migrationBuilder.DropColumn(
                name: "FechaNotaActualizada",
                table: "TurnosCaja");

            migrationBuilder.DropColumn(
                name: "NotaActualizadaPorEmpleadoId",
                table: "TurnosCaja");
        }
    }
}
