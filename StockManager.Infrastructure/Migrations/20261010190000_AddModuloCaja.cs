using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddModuloCaja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ConteoCiegoCaja",
                table: "Configuracion",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<int>(
                name: "TurnoCajaId",
                table: "Ventas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TurnoCajaId",
                table: "AbonosCuenta",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TurnoCajaId",
                table: "AbonosCuentaPorPagar",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "TurnosCaja",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    FechaApertura = table.Column<DateTime>(type: "datetime2", nullable: false),
                    BaseInicial = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaCierre = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CerradoPorEmpleadoId = table.Column<int>(type: "int", nullable: true),
                    EfectivoContado = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    EfectivoEsperado = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    Diferencia = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    NotaCierre = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    DetalleConteo = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TotalVentasEfectivo = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    TotalAbonosFiadoEfectivo = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    TotalPagosProveedorEfectivo = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    TotalTransferencias = table.Column<decimal>(type: "decimal(12,2)", nullable: true),
                    TotalTarjeta = table.Column<decimal>(type: "decimal(12,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TurnosCaja", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TurnosCaja_Empleados_CerradoPorEmpleadoId",
                        column: x => x.CerradoPorEmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TurnosCaja_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_TurnoCajaId",
                table: "Ventas",
                column: "TurnoCajaId");

            migrationBuilder.CreateIndex(
                name: "IX_AbonosCuenta_TurnoCajaId",
                table: "AbonosCuenta",
                column: "TurnoCajaId");

            migrationBuilder.CreateIndex(
                name: "IX_AbonosCuentaPorPagar_TurnoCajaId",
                table: "AbonosCuentaPorPagar",
                column: "TurnoCajaId");

            migrationBuilder.CreateIndex(
                name: "IX_TurnosCaja_CerradoPorEmpleadoId",
                table: "TurnosCaja",
                column: "CerradoPorEmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_TurnosCaja_EmpleadoId_Abierto",
                table: "TurnosCaja",
                column: "EmpleadoId",
                unique: true,
                filter: "[Estado] = 'Abierto'");

            migrationBuilder.CreateIndex(
                name: "IX_TurnosCaja_FechaApertura",
                table: "TurnosCaja",
                column: "FechaApertura");

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_TurnosCaja_TurnoCajaId",
                table: "Ventas",
                column: "TurnoCajaId",
                principalTable: "TurnosCaja",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AbonosCuenta_TurnosCaja_TurnoCajaId",
                table: "AbonosCuenta",
                column: "TurnoCajaId",
                principalTable: "TurnosCaja",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AbonosCuentaPorPagar_TurnosCaja_TurnoCajaId",
                table: "AbonosCuentaPorPagar",
                column: "TurnoCajaId",
                principalTable: "TurnosCaja",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(name: "FK_Ventas_TurnosCaja_TurnoCajaId", table: "Ventas");
            migrationBuilder.DropForeignKey(name: "FK_AbonosCuenta_TurnosCaja_TurnoCajaId", table: "AbonosCuenta");
            migrationBuilder.DropForeignKey(name: "FK_AbonosCuentaPorPagar_TurnosCaja_TurnoCajaId", table: "AbonosCuentaPorPagar");

            migrationBuilder.DropTable(name: "TurnosCaja");

            migrationBuilder.DropIndex(name: "IX_Ventas_TurnoCajaId", table: "Ventas");
            migrationBuilder.DropIndex(name: "IX_AbonosCuenta_TurnoCajaId", table: "AbonosCuenta");
            migrationBuilder.DropIndex(name: "IX_AbonosCuentaPorPagar_TurnoCajaId", table: "AbonosCuentaPorPagar");

            migrationBuilder.DropColumn(name: "TurnoCajaId", table: "Ventas");
            migrationBuilder.DropColumn(name: "TurnoCajaId", table: "AbonosCuenta");
            migrationBuilder.DropColumn(name: "TurnoCajaId", table: "AbonosCuentaPorPagar");
            migrationBuilder.DropColumn(name: "ConteoCiegoCaja", table: "Configuracion");
        }
    }
}
