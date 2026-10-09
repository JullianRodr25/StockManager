using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDatosFacturacionAConfiguracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActividadEconomicaEmpresa",
                table: "Configuracion",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BarrioEmpresa",
                table: "Configuracion",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CiudadEmpresa",
                table: "Configuracion",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PoliticaCambiosFactura",
                table: "Configuracion",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ResolucionDianFecha",
                table: "Configuracion",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolucionDianNumero",
                table: "Configuracion",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResolucionDianPrefijo",
                table: "Configuracion",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResolucionDianRangoDesde",
                table: "Configuracion",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResolucionDianRangoHasta",
                table: "Configuracion",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ResolucionDianVigenciaMeses",
                table: "Configuracion",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResponsabilidadIvaEmpresa",
                table: "Configuracion",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TextoLegalFactura",
                table: "Configuracion",
                type: "nvarchar(600)",
                maxLength: 600,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActividadEconomicaEmpresa",
                table: "Configuracion");

            migrationBuilder.DropColumn(
                name: "BarrioEmpresa",
                table: "Configuracion");

            migrationBuilder.DropColumn(
                name: "CiudadEmpresa",
                table: "Configuracion");

            migrationBuilder.DropColumn(
                name: "PoliticaCambiosFactura",
                table: "Configuracion");

            migrationBuilder.DropColumn(
                name: "ResolucionDianFecha",
                table: "Configuracion");

            migrationBuilder.DropColumn(
                name: "ResolucionDianNumero",
                table: "Configuracion");

            migrationBuilder.DropColumn(
                name: "ResolucionDianPrefijo",
                table: "Configuracion");

            migrationBuilder.DropColumn(
                name: "ResolucionDianRangoDesde",
                table: "Configuracion");

            migrationBuilder.DropColumn(
                name: "ResolucionDianRangoHasta",
                table: "Configuracion");

            migrationBuilder.DropColumn(
                name: "ResolucionDianVigenciaMeses",
                table: "Configuracion");

            migrationBuilder.DropColumn(
                name: "ResponsabilidadIvaEmpresa",
                table: "Configuracion");

            migrationBuilder.DropColumn(
                name: "TextoLegalFactura",
                table: "Configuracion");
        }
    }
}
