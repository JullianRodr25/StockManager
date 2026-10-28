using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using StockManager.Infrastructure.Data;

#nullable disable

namespace StockManager.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20261027210000_AddModuloNomina")]
    public partial class AddModuloNomina : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Trabajadores",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Activo = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    AplicaSeguridadSocial = table.Column<bool>(type: "bit", nullable: false),
                    Cargo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFinContrato = table.Column<DateTime>(type: "date", nullable: true),
                    FechaIngreso = table.Column<DateTime>(type: "date", nullable: false),
                    MetodoPago = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NumeroIdentificacion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SalarioMensual = table.Column<decimal>(type: "decimal(12, 2)", nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    TipoContrato = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Trabajadores", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Trabajadores_NumeroIdentificacion",
                table: "Trabajadores",
                column: "NumeroIdentificacion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Trabajadores_Activo",
                table: "Trabajadores",
                column: "Activo");

            migrationBuilder.CreateTable(
                name: "ParametrosNomina",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AuxilioTransporte = table.Column<decimal>(type: "decimal(12, 2)", nullable: false),
                    FechaActualizacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LimiteSalariosAuxilio = table.Column<decimal>(type: "decimal(6, 2)", nullable: false),
                    PorcentajePensionTrabajador = table.Column<decimal>(type: "decimal(5, 2)", nullable: false),
                    PorcentajeSaludTrabajador = table.Column<decimal>(type: "decimal(5, 2)", nullable: false),
                    SalarioMinimo = table.Column<decimal>(type: "decimal(12, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametrosNomina", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Prestamos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CuotasDescontadas = table.Column<int>(type: "int", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Monto = table.Column<decimal>(type: "decimal(12, 2)", nullable: false),
                    Nota = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    NumeroCuotas = table.Column<int>(type: "int", nullable: false),
                    PrimeraCuotaDesde = table.Column<DateTime>(type: "date", nullable: false),
                    SaldoPendiente = table.Column<decimal>(type: "decimal(12, 2)", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TrabajadorId = table.Column<int>(type: "int", nullable: false),
                    ValorCuota = table.Column<decimal>(type: "decimal(12, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prestamos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prestamos_Trabajadores_TrabajadorId",
                        column: x => x.TrabajadorId,
                        principalTable: "Trabajadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Prestamos_TrabajadorId_Estado",
                table: "Prestamos",
                columns: new[] { "TrabajadorId", "Estado" });

            migrationBuilder.CreateTable(
                name: "PeriodosNomina",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AprobadaEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AprobadaPorEmpleadoId = table.Column<int>(type: "int", nullable: true),
                    CerradaEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaFin = table.Column<DateTime>(type: "date", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "date", nullable: false),
                    PagadaEn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeriodosNomina", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PeriodosNomina_Empleados_AprobadaPorEmpleadoId",
                        column: x => x.AprobadaPorEmpleadoId,
                        principalTable: "Empleados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PeriodosNomina_AprobadaPorEmpleadoId",
                table: "PeriodosNomina",
                column: "AprobadaPorEmpleadoId");

            migrationBuilder.CreateIndex(
                name: "IX_PeriodosNomina_FechaInicio",
                table: "PeriodosNomina",
                column: "FechaInicio",
                unique: true);

            migrationBuilder.CreateTable(
                name: "LiquidacionesNomina",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AuxilioTransporte = table.Column<decimal>(type: "decimal(12, 2)", nullable: false),
                    Cargo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DescuentoPension = table.Column<decimal>(type: "decimal(12, 2)", nullable: false),
                    DescuentoPrestamos = table.Column<decimal>(type: "decimal(12, 2)", nullable: false),
                    DescuentoSalud = table.Column<decimal>(type: "decimal(12, 2)", nullable: false),
                    Devengado = table.Column<decimal>(type: "decimal(12, 2)", nullable: false),
                    MetodoPago = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Neto = table.Column<decimal>(type: "decimal(12, 2)", nullable: false),
                    NombreTrabajador = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    OtrosDescuentos = table.Column<decimal>(type: "decimal(12, 2)", nullable: false),
                    OtrosDevengos = table.Column<decimal>(type: "decimal(12, 2)", nullable: false),
                    Pagada = table.Column<bool>(type: "bit", nullable: false),
                    PagadaEn = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PeriodoNominaId = table.Column<int>(type: "int", nullable: false),
                    SalarioQuincena = table.Column<decimal>(type: "decimal(12, 2)", nullable: false),
                    TrabajadorId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LiquidacionesNomina", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LiquidacionesNomina_PeriodosNomina_PeriodoNominaId",
                        column: x => x.PeriodoNominaId,
                        principalTable: "PeriodosNomina",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LiquidacionesNomina_Trabajadores_TrabajadorId",
                        column: x => x.TrabajadorId,
                        principalTable: "Trabajadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LiquidacionesNomina_PeriodoNominaId_TrabajadorId",
                table: "LiquidacionesNomina",
                columns: new[] { "PeriodoNominaId", "TrabajadorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LiquidacionesNomina_TrabajadorId",
                table: "LiquidacionesNomina",
                column: "TrabajadorId");

            migrationBuilder.CreateTable(
                name: "NovedadesNomina",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Concepto = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodoNominaId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TrabajadorId = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(12, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NovedadesNomina", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NovedadesNomina_PeriodosNomina_PeriodoNominaId",
                        column: x => x.PeriodoNominaId,
                        principalTable: "PeriodosNomina",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NovedadesNomina_Trabajadores_TrabajadorId",
                        column: x => x.TrabajadorId,
                        principalTable: "Trabajadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NovedadesNomina_PeriodoNominaId_TrabajadorId",
                table: "NovedadesNomina",
                columns: new[] { "PeriodoNominaId", "TrabajadorId" });

            migrationBuilder.CreateIndex(
                name: "IX_NovedadesNomina_TrabajadorId",
                table: "NovedadesNomina",
                column: "TrabajadorId");

            migrationBuilder.CreateTable(
                name: "CuotasPrestamo",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Aplicada = table.Column<bool>(type: "bit", nullable: false),
                    Numero = table.Column<int>(type: "int", nullable: false),
                    PeriodoNominaId = table.Column<int>(type: "int", nullable: false),
                    PrestamoId = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(12, 2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CuotasPrestamo", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CuotasPrestamo_PeriodosNomina_PeriodoNominaId",
                        column: x => x.PeriodoNominaId,
                        principalTable: "PeriodosNomina",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CuotasPrestamo_Prestamos_PrestamoId",
                        column: x => x.PrestamoId,
                        principalTable: "Prestamos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CuotasPrestamo_PeriodoNominaId_PrestamoId",
                table: "CuotasPrestamo",
                columns: new[] { "PeriodoNominaId", "PrestamoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CuotasPrestamo_PrestamoId",
                table: "CuotasPrestamo",
                column: "PrestamoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CuotasPrestamo");

            migrationBuilder.DropTable(
                name: "NovedadesNomina");

            migrationBuilder.DropTable(
                name: "LiquidacionesNomina");

            migrationBuilder.DropTable(
                name: "PeriodosNomina");

            migrationBuilder.DropTable(
                name: "Prestamos");

            migrationBuilder.DropTable(
                name: "ParametrosNomina");

            migrationBuilder.DropTable(
                name: "Trabajadores");
        }
    }
}
