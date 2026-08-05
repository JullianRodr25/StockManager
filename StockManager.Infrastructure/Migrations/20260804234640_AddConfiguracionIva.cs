using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConfiguracionIva : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "TarifaIva",
                table: "Productos",
                type: "decimal(5,2)",
                nullable: false,
                defaultValue: 19.00m);

            migrationBuilder.CreateTable(
                name: "Configuracion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TarifaIvaPorDefecto = table.Column<decimal>(type: "decimal(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configuracion", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "Configuracion",
                columns: new[] { "Id", "TarifaIvaPorDefecto" },
                values: new object[] { 1, 19.00m });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Producto_TarifaIva_Between_Zero_And_OneHundred",
                table: "Productos",
                sql: "[TarifaIva] >= 0 AND [TarifaIva] <= 100");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Configuracion");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Producto_TarifaIva_Between_Zero_And_OneHundred",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "TarifaIva",
                table: "Productos");
        }
    }
}
