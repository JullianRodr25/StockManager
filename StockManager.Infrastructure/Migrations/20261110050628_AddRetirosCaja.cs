using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRetirosCaja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TotalRetirosEfectivo",
                table: "TurnosCaja",
                type: "decimal(12,2)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RetirosCaja",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TurnoCajaId = table.Column<int>(type: "int", nullable: false),
                    EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Anulado = table.Column<bool>(type: "bit", nullable: false),
                    AnuladoPorEmpleadoId = table.Column<int>(type: "int", nullable: true),
                    FechaAnulacion = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RetirosCaja", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RetirosCaja_Empleados_AnuladoPorEmpleadoId",
                        column: x => x.AnuladoPorEmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RetirosCaja_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RetirosCaja_TurnosCaja_TurnoCajaId",
                        column: x => x.TurnoCajaId,
                        principalTable: "TurnosCaja",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RetirosCaja_AnuladoPorEmpleadoId",
                table: "RetirosCaja",
                column: "AnuladoPorEmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_RetirosCaja_EmpleadoId",
                table: "RetirosCaja",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_RetirosCaja_Fecha",
                table: "RetirosCaja",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_RetirosCaja_TurnoCajaId",
                table: "RetirosCaja",
                column: "TurnoCajaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RetirosCaja");

            migrationBuilder.DropColumn(
                name: "TotalRetirosEfectivo",
                table: "TurnosCaja");
        }
    }
}
