using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCostoYAplicaIvaAProducto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AplicaIva",
                table: "Productos",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Costo",
                table: "Productos",
                type: "decimal(12, 2)",
                nullable: false,
                defaultValue: 0m);

            // Los productos existentes mantienen su TarifaIva actual: si ya tenían 0% quedan
            // marcados como "no aplica IVA" (coherente con la invariante del dominio); el resto
            // queda con AplicaIva = true (el valor por defecto de la columna), que es correcto
            // porque hoy toda la operación asume que el IVA siempre aplica.
            migrationBuilder.Sql(
                "UPDATE [Productos] SET [AplicaIva] = 0 WHERE [TarifaIva] = 0;");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Producto_Costo_GreaterOrEqual_Zero",
                table: "Productos",
                sql: "[Costo] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Producto_Costo_GreaterOrEqual_Zero",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "AplicaIva",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "Costo",
                table: "Productos");
        }
    }
}
