using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MakeFacturaNumeroNullable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Facturas_Numero",
                table: "Facturas");

            migrationBuilder.AlterColumn<string>(
                name: "Numero",
                table: "Facturas",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_Numero",
                table: "Facturas",
                column: "Numero",
                unique: true,
                filter: "[Numero] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Facturas_Numero",
                table: "Facturas");

            migrationBuilder.AlterColumn<string>(
                name: "Numero",
                table: "Facturas",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_Numero",
                table: "Facturas",
                column: "Numero",
                unique: true);
        }
    }
}
