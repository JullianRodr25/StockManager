using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDatosEmpresaAConfiguracion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NombreEmpresa",
                table: "Configuracion",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NitEmpresa",
                table: "Configuracion",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DireccionEmpresa",
                table: "Configuracion",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TelefonoEmpresa",
                table: "Configuracion",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmailEmpresa",
                table: "Configuracion",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NombreEmpresa",
                table: "Configuracion");

            migrationBuilder.DropColumn(
                name: "NitEmpresa",
                table: "Configuracion");

            migrationBuilder.DropColumn(
                name: "DireccionEmpresa",
                table: "Configuracion");

            migrationBuilder.DropColumn(
                name: "TelefonoEmpresa",
                table: "Configuracion");

            migrationBuilder.DropColumn(
                name: "EmailEmpresa",
                table: "Configuracion");
        }
    }
}
