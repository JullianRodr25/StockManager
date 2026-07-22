using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNumeroIdentificacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NumeroIdentificacion",
                table: "Empleados",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NumeroIdentificacion",
                table: "Clientes",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Empleados_NumeroIdentificacion",
                table: "Empleados",
                column: "NumeroIdentificacion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_NumeroIdentificacion",
                table: "Clientes",
                column: "NumeroIdentificacion",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Empleados_NumeroIdentificacion",
                table: "Empleados");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_NumeroIdentificacion",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "NumeroIdentificacion",
                table: "Empleados");

            migrationBuilder.DropColumn(
                name: "NumeroIdentificacion",
                table: "Clientes");
        }
    }
}
