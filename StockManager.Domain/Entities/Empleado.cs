using StockManager.Domain.Constants;

namespace StockManager.Domain.Entities;

/// <summary>
/// Entidad Empleado del dominio.
/// Representa un trabajador de la ferretería.
/// </summary>
public class Empleado
{
    public int Id { get; private set; }
    public string NumeroIdentificacion { get; private set; } = null!;  // Cédula, Pasaporte, etc.
    public string Nombre { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string Rol { get; private set; } = null!;  // 'Admin' | 'Empleado' | 'ConsultaInventario' (ver Roles)
    public bool Activo { get; private set; }

    private Empleado() { }

    public static Empleado Crear(string numeroIdentificacion, string nombre, string email, string passwordHash, string rol)
    {
        if (string.IsNullOrWhiteSpace(numeroIdentificacion))
            throw new ArgumentException("El número de identificación no puede estar vacío.", nameof(numeroIdentificacion));

        if (numeroIdentificacion.Length > 50)
            throw new ArgumentException("El número de identificación no puede exceder 50 caracteres.", nameof(numeroIdentificacion));

        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del empleado no puede estar vacío.", nameof(nombre));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("El email no puede estar vacío.", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("El hash de contraseña no puede estar vacío.", nameof(passwordHash));

        if (!Roles.EsRolDeEmpleado(rol))
            throw new ArgumentException("El rol debe ser 'Admin', 'Empleado' o 'ConsultaInventario'.", nameof(rol));

        return new Empleado
        {
            NumeroIdentificacion = numeroIdentificacion.Trim(),
            Nombre = nombre.Trim(),
            Email = email.Trim().ToLower(),
            PasswordHash = passwordHash,
            Rol = rol,
            Activo = true
        };
    }

    public void Desactivar() => Activo = false;
    public void Activar() => Activo = true;

    /// <summary>
    /// Reemplaza el hash de contraseña del empleado. Usado tanto por un cambio de contraseña
    /// autenticado como por el flujo de recuperación por correo (token firmado, ver
    /// IPasswordResetTokenService). La validación de la contraseña en texto plano (longitud,
    /// etc.) ocurre antes, en la capa de aplicación; aquí solo se persiste el hash ya calculado.
    /// </summary>
    public void ActualizarPasswordHash(string nuevoHash)
    {
        if (string.IsNullOrWhiteSpace(nuevoHash))
            throw new ArgumentException("El hash de contraseña no puede estar vacío.", nameof(nuevoHash));

        PasswordHash = nuevoHash;
    }
}
