namespace StockManager.Domain.Constants;

/// <summary>
/// Roles de los empleados (claim de rol del JWT) y combinaciones reutilizables para
/// [Authorize(Roles = ...)]. Es la única fuente de estos nombres: validar un rol nuevo o
/// agregar uno se hace aquí y no en cada controlador.
/// </summary>
public static class Roles
{
    /// <summary>Acceso completo, incluida la configuración y la eliminación de productos.</summary>
    public const string Admin = "Admin";

    /// <summary>Opera el negocio (ventas, pedidos, clientes, proveedores) y consulta el inventario.</summary>
    public const string Empleado = "Empleado";

    /// <summary>
    /// Encargado de inventario: ve y crea productos y solo puede sumar stock (no edita, no resta,
    /// no usa Excel). No vende ni entra a ningún otro módulo. Cualquier endpoint que no lo nombre explícitamente le
    /// queda cerrado (lista blanca).
    /// </summary>
    public const string Inventario = "Inventario";

    /// <summary>Quienes operan el negocio (excluye al encargado de inventario).</summary>
    public const string AdminYEmpleado = Admin + "," + Empleado;

    /// <summary>Quienes pueden consultar el inventario: productos, categorías y datos de apoyo.</summary>
    public const string PersonalConInventario = Admin + "," + Empleado + "," + Inventario;

    /// <summary>Quienes pueden dar de alta productos y sumar stock (restar o editar es solo Admin).</summary>
    public const string AltaInventario = Admin + "," + Inventario;

    /// <summary>Un rol es válido para un empleado si es uno de los de personal.</summary>
    public static bool EsRolDeEmpleado(string? rol) =>
        rol is Admin or Empleado or Inventario;
}
