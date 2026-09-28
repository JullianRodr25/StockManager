using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockManager.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddVentaIdAPedidoYDetallePedidoIdABackorder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VentaId",
                table: "Pedidos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DetallePedidoId",
                table: "BackorderRequests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pedidos_VentaId",
                table: "Pedidos",
                column: "VentaId");

            migrationBuilder.CreateIndex(
                name: "IX_BackorderRequests_DetallePedidoId",
                table: "BackorderRequests",
                column: "DetallePedidoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pedidos_Ventas_VentaId",
                table: "Pedidos",
                column: "VentaId",
                principalTable: "Ventas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BackorderRequests_DetallesPedido_DetallePedidoId",
                table: "BackorderRequests",
                column: "DetallePedidoId",
                principalTable: "DetallesPedido",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pedidos_Ventas_VentaId",
                table: "Pedidos");

            migrationBuilder.DropForeignKey(
                name: "FK_BackorderRequests_DetallesPedido_DetallePedidoId",
                table: "BackorderRequests");

            migrationBuilder.DropIndex(
                name: "IX_Pedidos_VentaId",
                table: "Pedidos");

            migrationBuilder.DropIndex(
                name: "IX_BackorderRequests_DetallePedidoId",
                table: "BackorderRequests");

            migrationBuilder.DropColumn(
                name: "VentaId",
                table: "Pedidos");

            migrationBuilder.DropColumn(
                name: "DetallePedidoId",
                table: "BackorderRequests");
        }
    }
}
