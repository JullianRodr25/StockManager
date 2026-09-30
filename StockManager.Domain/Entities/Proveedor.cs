namespace StockManager.Domain.Entities;

/// <summary>
/// Entidad Proveedor del dominio.
/// Representa una empresa o persona a la que la ferretería le compra mercancía,
/// típicamente a crédito (ver CuentaPorPagar).
/// </summary>
public class Proveedor
{
    public int Id { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string? NumeroIdentificacion { get; private set; }  // NIT o cédula del proveedor
    public string? Telefono { get; private set; }
    public string? Email { get; private set; }
    public string? Direccion { get; private set; }
    public bool Activo { get; private set; }

    private Proveedor() { }

    public static Proveedor Crear(string nombre, string? numeroIdentificacion, string? telefono, string? email, string? direccion)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del proveedor no puede estar vacío.", nameof(nombre));

        if (nombre.Length > 200)
            throw new ArgumentException("El nombre del proveedor no puede exceder 200 caracteres.", nameof(nombre));

        if (!string.IsNullOrWhiteSpace(numeroIdentificacion) && numeroIdentificacion.Length > 50)
            throw new ArgumentException("El número de identificación no puede exceder 50 caracteres.", nameof(numeroIdentificacion));

        return new Proveedor
        {
            Nombre = nombre.Trim(),
            NumeroIdentificacion = string.IsNullOrWhiteSpace(numeroIdentificacion) ? null : numeroIdentificacion.Trim(),
            Telefono = string.IsNullOrWhiteSpace(telefono) ? null : telefono.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLower(),
            Direccion = string.IsNullOrWhiteSpace(direccion) ? null : direccion.Trim(),
            Activo = true
        };
    }

    public void ActualizarInformacion(string nombre, string? numeroIdentificacion, string? telefono, string? email, string? direccion)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del proveedor no puede estar vacío.", nameof(nombre));

        if (nombre.Length > 200)
            throw new ArgumentException("El nombre del proveedor no puede exceder 200 caracteres.", nameof(nombre));

        Nombre = nombre.Trim();
        NumeroIdentificacion = string.IsNullOrWhiteSpace(numeroIdentificacion) ? null : numeroIdentificacion.Trim();
        Telefono = string.IsNullOrWhiteSpace(telefono) ? null : telefono.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLower();
        Direccion = string.IsNullOrWhiteSpace(direccion) ? null : direccion.Trim();
    }

    public void Desactivar() => Activo = false;
    public void Activar() => Activo = true;
}
