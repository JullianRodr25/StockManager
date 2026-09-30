using System.Text.RegularExpressions;

namespace StockManager.Domain.Entities;

/// <summary>
/// Entidad Proveedor del dominio.
/// Representa una empresa o persona a la que la ferretería le compra mercancía,
/// típicamente a crédito (ver CuentaPorPagar).
/// </summary>
public class Proveedor
{
    // Formato internacional E.164: '+' seguido de 8 a 15 dígitos, sin espacios ni separadores.
    // Mismo patrón que Configuracion.TelefonoNotificacionesAdmin.
    private static readonly Regex FormatoTelefonoE164 = new(@"^\+[1-9]\d{7,14}$", RegexOptions.Compiled);

    public int Id { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string? NumeroIdentificacion { get; private set; }  // NIT o cédula del proveedor
    public string? Telefono { get; private set; }
    public string? Email { get; private set; }
    public string? Direccion { get; private set; }
    public bool Activo { get; private set; }

    /// <summary>
    /// Número de WhatsApp (formato E.164) del proveedor, usado para avisarle automáticamente
    /// cuando alguno de los productos que le compramos entra en stock bajo. Null o vacío
    /// desactiva ese aviso para este proveedor sin afectar el resto del sistema.
    /// </summary>
    public string? NumeroWhatsApp { get; private set; }

    /// <summary>
    /// Última vez que se le avisó por WhatsApp sobre stock bajo. Evita que el chequeo
    /// periódico reenvíe la misma alerta más de una vez por día.
    /// </summary>
    public DateTime? FechaUltimaAlertaStockBajo { get; private set; }

    private Proveedor() { }

    public static Proveedor Crear(string nombre, string? numeroIdentificacion, string? telefono, string? email, string? direccion, string? numeroWhatsApp = null)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del proveedor no puede estar vacío.", nameof(nombre));

        if (nombre.Length > 200)
            throw new ArgumentException("El nombre del proveedor no puede exceder 200 caracteres.", nameof(nombre));

        if (!string.IsNullOrWhiteSpace(numeroIdentificacion) && numeroIdentificacion.Length > 50)
            throw new ArgumentException("El número de identificación no puede exceder 50 caracteres.", nameof(numeroIdentificacion));

        var proveedor = new Proveedor
        {
            Nombre = nombre.Trim(),
            NumeroIdentificacion = string.IsNullOrWhiteSpace(numeroIdentificacion) ? null : numeroIdentificacion.Trim(),
            Telefono = string.IsNullOrWhiteSpace(telefono) ? null : telefono.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLower(),
            Direccion = string.IsNullOrWhiteSpace(direccion) ? null : direccion.Trim(),
            Activo = true
        };
        proveedor.ActualizarNumeroWhatsApp(numeroWhatsApp);
        return proveedor;
    }

    public void ActualizarInformacion(string nombre, string? numeroIdentificacion, string? telefono, string? email, string? direccion, string? numeroWhatsApp)
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
        ActualizarNumeroWhatsApp(numeroWhatsApp);
    }

    public void ActualizarNumeroWhatsApp(string? nuevoNumero)
    {
        var numero = string.IsNullOrWhiteSpace(nuevoNumero) ? null : nuevoNumero.Trim();

        if (numero is not null && !FormatoTelefonoE164.IsMatch(numero))
            throw new ArgumentException("El número de WhatsApp debe estar en formato internacional E.164, ej. +573001234567.");

        NumeroWhatsApp = numero;
    }

    public void RegistrarAlertaStockBajoEnviada()
    {
        FechaUltimaAlertaStockBajo = DateTime.UtcNow;
    }

    public void Desactivar() => Activo = false;
    public void Activar() => Activo = true;
}
