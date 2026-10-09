namespace StockManager.Domain.Constants;

/// <summary>
/// Roles de los empleados (claim de rol del JWT) y combinaciones reutilizables para
/// [Authorize(Roles = ...)]. Es la única fuente de estos nombres: validar un rol nuevo o
/// agregar uno se hace aquí y no en cada controlador.
/// </summary>
public static class Roles
{
    /// <summary>Acceso completo, incluida la configuración y la edición del inventario.</summary>
    public const string Admin = "Admin";

    /// <summary>Opera el negocio (ventas, pedidos, clientes, proveedores) y consulta el inventario.</summary>
    public const string Empleado = "Empleado";

    /// <summary>
    /// Solo consulta el inventario: no vende, no ve costos ni proveedores y no modifica nada.
    /// Cualquier endpoint que no lo nombre explícitamente le queda cerrado (lista blanca).
    /// </summary>
    public const string ConsultaInventario = "ConsultaInventario";

    /// <summary>Quienes operan el negocio (excluye al rol de solo consulta).</summary>
    public const string AdminYEmpleado = Admin + "," + Empleado;

    /// <summary>Quienes pueden leer el inventario (productos y categorías).</summary>
    public const string LecturaInventario = Admin + "," + Empleado + "," + ConsultaInventario;

    /// <summary>Un rol es válido para un empleado si es uno de los de personal.</summary>
    public static bool EsRolDeEmpleado(string? rol) =>
        rol is Admin or Empleado or ConsultaInventario;
}
