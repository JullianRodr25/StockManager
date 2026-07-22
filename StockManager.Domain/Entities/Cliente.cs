namespace StockManager.Domain.Entities;

/// <summary>
/// Entidad Cliente del dominio.
/// Representa un cliente que compra en la ferretería.
/// </summary>
public class Cliente
{
    public int Id { get; private set; }
    public string NumeroIdentificacion { get; private set; } = null!;  // Cédula, Pasaporte, etc.
    public string Nombre { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string Telefono { get; private set; } = null!;  // Usado para WhatsApp
    public string Direccion { get; private set; } = null!;
    public bool Activo { get; private set; }

    private Cliente() { }

    public static Cliente Crear(string numeroIdentificacion, string nombre, string email, string passwordHash, string telefono, string direccion)
    {
        if (string.IsNullOrWhiteSpace(numeroIdentificacion))
            throw new ArgumentException("El número de identificación no puede estar vacío.", nameof(numeroIdentificacion));

        if (numeroIdentificacion.Length > 50)
            throw new ArgumentException("El número de identificación no puede exceder 50 caracteres.", nameof(numeroIdentificacion));

        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del cliente no puede estar vacío.", nameof(nombre));

        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("El email no puede estar vacío.", nameof(email));

        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("El hash de contraseña no puede estar vacío.", nameof(passwordHash));

        if (string.IsNullOrWhiteSpace(telefono))
            throw new ArgumentException("El teléfono no puede estar vacío.", nameof(telefono));

        if (string.IsNullOrWhiteSpace(direccion))
            throw new ArgumentException("La dirección no puede estar vacía.", nameof(direccion));

        return new Cliente
        {
            NumeroIdentificacion = numeroIdentificacion.Trim(),
            Nombre = nombre.Trim(),
            Email = email.Trim().ToLower(),
            PasswordHash = passwordHash,
            Telefono = telefono.Trim(),
            Direccion = direccion.Trim(),
            Activo = true
        };
    }

    public void Desactivar() => Activo = false;
    public void Activar() => Activo = true;
}
