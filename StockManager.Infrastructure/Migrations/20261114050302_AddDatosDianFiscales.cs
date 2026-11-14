using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDatosDianFiscales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CodigoMunicipioFacturado",
                table: "Ventas",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoPersonaFacturada",
                table: "Ventas",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnidadMedidaDian",
                table: "Productos",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: false,
                defaultValue: "94");

            migrationBuilder.AddColumn<string>(
                name: "CodigoMunicipioEmpresa",
                table: "Configuracion",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CodigoMunicipioDane",
                table: "Clientes",
                type: "nvarchar(5)",
                maxLength: 5,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TipoPersona",
                table: "Clientes",
                type: "nvarchar(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "Configuracion",
                keyColumn: "Id",
                keyValue: 1,
                column: "CodigoMunicipioEmpresa",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CodigoMunicipioFacturado",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "TipoPersonaFacturada",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "UnidadMedidaDian",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "CodigoMunicipioEmpresa",
                table: "Configuracion");

            migrationBuilder.DropColumn(
                name: "CodigoMunicipioDane",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "TipoPersona",
                table: "Clientes");
        }
    }
}
