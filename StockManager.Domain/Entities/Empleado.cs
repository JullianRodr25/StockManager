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
    public string Rol { get; private set; } = null!;  // 'Admin' | 'Empleado'
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

        if (rol != "Admin" && rol != "Empleado")
            throw new ArgumentException("El rol debe ser 'Admin' o 'Empleado'.", nameof(rol));

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
}
