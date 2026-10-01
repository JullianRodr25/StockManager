using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFacturaElectronicaClienteYVenta : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // --- Clientes: origen de registro + datos fiscales ---
            migrationBuilder.AddColumn<string>(
                name: "OrigenRegistro",
                table: "Clientes",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "Caja");

            migrationBuilder.AddColumn<string>(
                name: "TipoDocumentoFiscal",
                table: "Clientes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroDocumentoFiscal",
                table: "Clientes",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RazonSocialFiscal",
                table: "Clientes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DireccionFiscal",
                table: "Clientes",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailFacturacion",
                table: "Clientes",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            // --- Ventas: solicitud de factura electrónica (snapshot fiscal + estado) ---
            migrationBuilder.AddColumn<bool>(
                name: "RequiereFacturaElectronica",
                table: "Ventas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "EstadoFacturaElectronica",
                table: "Ventas",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "NoAplica");

            migrationBuilder.AddColumn<string>(
                name: "TipoDocumentoFacturado",
                table: "Ventas",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroDocumentoFacturado",
                table: "Ventas",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RazonSocialFacturada",
                table: "Ventas",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DireccionFacturada",
                table: "Ventas",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailFacturacion",
                table: "Ventas",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "OrigenRegistro", table: "Clientes");
            migrationBuilder.DropColumn(name: "TipoDocumentoFiscal", table: "Clientes");
            migrationBuilder.DropColumn(name: "NumeroDocumentoFiscal", table: "Clientes");
            migrationBuilder.DropColumn(name: "RazonSocialFiscal", table: "Clientes");
            migrationBuilder.DropColumn(name: "DireccionFiscal", table: "Clientes");
            migrationBuilder.DropColumn(name: "EmailFacturacion", table: "Clientes");

            migrationBuilder.DropColumn(name: "RequiereFacturaElectronica", table: "Ventas");
            migrationBuilder.DropColumn(name: "EstadoFacturaElectronica", table: "Ventas");
            migrationBuilder.DropColumn(name: "TipoDocumentoFacturado", table: "Ventas");
            migrationBuilder.DropColumn(name: "NumeroDocumentoFacturado", table: "Ventas");
            migrationBuilder.DropColumn(name: "RazonSocialFacturada", table: "Ventas");
            migrationBuilder.DropColumn(name: "DireccionFacturada", table: "Ventas");
            migrationBuilder.DropColumn(name: "EmailFacturacion", table: "Ventas");
        }
    }
}
