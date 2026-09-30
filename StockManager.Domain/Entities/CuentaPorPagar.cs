namespace StockManager.Domain.Entities;

using StockManager.Domain.Exceptions;

/// <summary>
/// Entidad CuentaPorPagar del dominio.
/// Representa una compra a crédito hecha a un Proveedor: cuánto se le debe, desde cuándo
/// y para cuándo. "Vencida" NO se persiste como estado propio — se deriva comparando
/// FechaVencimiento contra la fecha actual (ver servicios de aplicación) — así el estado
/// nunca puede quedar desincronizado esperando que un job pase a marcarlo.
/// </summary>
public class CuentaPorPagar
{
    private static readonly string[] EstadosValidos = { "Pendiente", "Pagada", "Cancelada" };

    public int Id { get; private set; }
    public int ProveedorId { get; private set; }
    public string Concepto { get; private set; } = null!;
    public decimal MontoTotal { get; private set; }
    public DateTime FechaCompra { get; private set; }
    public DateTime FechaVencimiento { get; private set; }
    public string Estado { get; private set; } = null!;

    /// <summary>
    /// Última vez que se avisó por WhatsApp que esta cuenta está por vencer o ya venció.
    /// Evita que el chequeo periódico reenvíe la misma alerta más de una vez por día.
    /// </summary>
    public DateTime? FechaUltimaAlerta { get; private set; }

    private CuentaPorPagar() { }

    public static CuentaPorPagar Crear(int proveedorId, string concepto, decimal montoTotal, DateTime fechaVencimiento)
    {
        if (proveedorId <= 0)
            throw new ArgumentException("ProveedorId debe ser mayor a 0.", nameof(proveedorId));

        if (string.IsNullOrWhiteSpace(concepto))
            throw new ArgumentException("El concepto no puede estar vacío.", nameof(concepto));

        if (concepto.Length > 300)
            throw new ArgumentException("El concepto no puede exceder 300 caracteres.", nameof(concepto));

        if (montoTotal <= 0)
            throw new ArgumentException("El monto total debe ser mayor a 0.", nameof(montoTotal));

        return new CuentaPorPagar
        {
            ProveedorId = proveedorId,
            Concepto = concepto.Trim(),
            MontoTotal = montoTotal,
            FechaCompra = DateTime.UtcNow,
            FechaVencimiento = fechaVencimiento.Date,
            Estado = "Pendiente"
        };
    }

    /// <summary>
    /// Marca la cuenta como pagada. La validación de que el saldo (Total - abonos) llegó a
    /// cero es responsabilidad del servicio de aplicación, igual que en Venta/AbonoCuenta.
    /// </summary>
    public void MarcarPagada()
    {
        if (Estado != "Pendiente")
            throw new CuentaPorPagarEstadoInvalidoException(Id, Estado, "Pendiente");

        Estado = "Pagada";
    }

    /// <summary>
    /// Cancela la cuenta (ej. la compra se devolvió). Solo permitido sin abonos registrados,
    /// validación que igualmente corresponde al servicio de aplicación.
    /// </summary>
    public void Cancelar()
    {
        if (Estado != "Pendiente")
            throw new CuentaPorPagarEstadoInvalidoException(Id, Estado, "Pendiente");

        Estado = "Cancelada";
    }

    public void RegistrarAlertaEnviada()
    {
        FechaUltimaAlerta = DateTime.UtcNow;
    }
}
