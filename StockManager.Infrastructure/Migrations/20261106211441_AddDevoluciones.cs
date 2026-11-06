using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDevoluciones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Devoluciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VentaId = table.Column<int>(type: "int", nullable: false),
                    EmpleadoId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Motivo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TotalDevuelto = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    TotalCambio = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    MetodoPago = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TurnoCajaId = table.Column<int>(type: "int", nullable: true),
                    RequiereNotaCredito = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Devoluciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Devoluciones_Empleados_EmpleadoId",
                        column: x => x.EmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Devoluciones_TurnosCaja_TurnoCajaId",
                        column: x => x.TurnoCajaId,
                        principalTable: "TurnosCaja",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Devoluciones_Ventas_VentaId",
                        column: x => x.VentaId,
                        principalTable: "Ventas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DetallesDevolucion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DevolucionId = table.Column<int>(type: "int", nullable: false),
                    Sentido = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DetalleVentaId = table.Column<int>(type: "int", nullable: true),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    Cantidad = table.Column<int>(type: "int", nullable: false),
                    PrecioUnitario = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    ReintegradoAlStock = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesDevolucion", x => x.Id);
                    table.CheckConstraint("CK_DetalleDevolucion_Cantidad_Positiva", "[Cantidad] > 0");
                    table.ForeignKey(
                        name: "FK_DetallesDevolucion_DetallesVenta_DetalleVentaId",
                        column: x => x.DetalleVentaId,
                        principalTable: "DetallesVenta",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DetallesDevolucion_Devoluciones_DevolucionId",
                        column: x => x.DevolucionId,
                        principalTable: "Devoluciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetallesDevolucion_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DetallesDevolucion_DetalleVentaId",
                table: "DetallesDevolucion",
                column: "DetalleVentaId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesDevolucion_DevolucionId",
                table: "DetallesDevolucion",
                column: "DevolucionId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesDevolucion_ProductoId",
                table: "DetallesDevolucion",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Devoluciones_EmpleadoId",
                table: "Devoluciones",
                column: "EmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Devoluciones_Fecha",
                table: "Devoluciones",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Devoluciones_TurnoCajaId",
                table: "Devoluciones",
                column: "TurnoCajaId");

            migrationBuilder.CreateIndex(
                name: "IX_Devoluciones_VentaId",
                table: "Devoluciones",
                column: "VentaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DetallesDevolucion");

            migrationBuilder.DropTable(
                name: "Devoluciones");
        }
    }
}
