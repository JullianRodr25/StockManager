using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using StockManager.Infrastructure.Data;

#nullable disable

namespace StockManager.Infrastructure.Migrations
{
    /// <summary>
    /// Migración solo de datos (el modelo no cambia, por eso el snapshot queda igual): el rol
    /// "Empleado" pasa a llamarse "Caja". Se actualiza la columna para que los usuarios existentes
    /// conserven sus permisos con el nombre nuevo.
    /// </summary>
    [DbContext(typeof(AppDbContext))]
    [Migration("20261104210000_RenombrarRolEmpleadoACaja")]
    public partial class RenombrarRolEmpleadoACaja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Empleados SET Rol = 'Caja' WHERE Rol = 'Empleado';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Empleados SET Rol = 'Empleado' WHERE Rol = 'Caja';");
        }
    }
}
