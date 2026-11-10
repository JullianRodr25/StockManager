using StockManager.Domain.Exceptions;

namespace StockManager.Domain.Entities;

/// <summary>
/// Cotización: una oferta de precios con plazo de validez. Es un agregado propio y NO una Venta
/// con una bandera, porque sus reglas son opuestas a las de una venta: no toca inventario ni
/// caja, no genera factura, es editable y vence. Al aceptarla se convierte en una Venta real
/// (ver VentaService.RegistrarVentaDesdeCotizacionAsync) y queda enlazada a ella.
///
/// Estados guardados: Vigente, Convertida, Anulada. "Vencida" NO se guarda: se deduce de la
/// fecha de vencimiento (<see cref="EstadoEfectivo"/>), así no hace falta ningún proceso que la
/// marque y una cotización vencida puede revivirse editando su plazo.
/// </summary>
public class Cotizacion
{
    public const string EstadoVigente = "Vigente";
    public const string EstadoConvertida = "Convertida";
    public const string EstadoAnulada = "Anulada";
    public const string EstadoVencida = "Vencida";

    public const int MaximoCaracteresNotas = 500;

    private readonly List<DetalleCotizacion> _detalles = new();

    public int Id { get; private set; }
    public int EmpleadoId { get; private set; }
    public int? ClienteId { get; private set; }
    public string? NombreCliente { get; private set; }
    public string? TelefonoCliente { get; private set; }
    public string? EmailCliente { get; private set; }

    public DateTime Fecha { get; private set; }

    /// <summary>Último día (calendario de Colombia) en que la oferta es válida, inclusive.</summary>
    public DateOnly FechaVencimiento { get; private set; }

    public string? Notas { get; private set; }
    public decimal Total { get; private set; }
    public string Estado { get; private set; } = EstadoVigente;

    public int? VentaId { get; private set; }
    public DateTime? FechaConversion { get; private set; }
    public DateTime? FechaAnulacion { get; private set; }
    public DateTime? FechaActualizacion { get; private set; }

    public IReadOnlyList<DetalleCotizacion> Detalles => _detalles;

    private Cotizacion() { }

    public static Cotizacion Crear(
        int empleadoId,
        int? clienteId,
        string? nombreCliente,
        string? telefonoCliente,
        string? emailCliente,
        DateOnly fechaVencimiento,
        string? notas,
        IReadOnlyList<(int ProductoId, int Cantidad, decimal PrecioUnitario)> lineas,
        DateOnly hoy)
    {
        if (empleadoId <= 0)
            throw new ArgumentException("EmpleadoId debe ser mayor a 0.", nameof(empleadoId));

        var cotizacion = new Cotizacion
        {
            EmpleadoId = empleadoId,
            Fecha = DateTime.UtcNow,
            Estado = EstadoVigente
        };

        cotizacion.AplicarContenido(clienteId, nombreCliente, telefonoCliente, emailCliente,
            fechaVencimiento, notas, lineas, hoy);
        return cotizacion;
    }

    /// <summary>
    /// Reemplaza el contenido de la cotización. Solo mientras esté Vigente (aunque ya haya
    /// vencido: editar el plazo es justamente la forma de reactivarla).
    /// </summary>
    public void Editar(
        int? clienteId,
        string? nombreCliente,
        string? telefonoCliente,
        string? emailCliente,
        DateOnly fechaVencimiento,
        string? notas,
        IReadOnlyList<(int ProductoId, int Cantidad, decimal PrecioUnitario)> lineas,
        DateOnly hoy)
    {
        ExigirVigente("editar");
        AplicarContenido(clienteId, nombreCliente, telefonoCliente, emailCliente,
            fechaVencimiento, notas, lineas, hoy);
        FechaActualizacion = DateTime.UtcNow;
    }

    public void Anular()
    {
        ExigirVigente("anular");
        Estado = EstadoAnulada;
        FechaAnulacion = DateTime.UtcNow;
    }

    /// <summary>Marca la cotización como aceptada y enlazada a la venta creada a partir de ella.</summary>
    public void MarcarConvertida(int ventaId, DateOnly hoy)
    {
        ExigirVigente("convertir en venta");

        if (EstaVencida(hoy))
            throw new CotizacionNoModificableException(
                $"La cotización venció el {FechaVencimiento:dd/MM/yyyy}. Edita su plazo para volver a activarla.");

        if (ventaId <= 0)
            throw new ArgumentException("VentaId debe ser mayor a 0.", nameof(ventaId));

        Estado = EstadoConvertida;
        VentaId = ventaId;
        FechaConversion = DateTime.UtcNow;
    }

    public bool EstaVencida(DateOnly hoy) => Estado == EstadoVigente && hoy > FechaVencimiento;

    /// <summary>Estado que ve el usuario: el guardado, o "Vencida" si siguió Vigente más allá del plazo.</summary>
    public string EstadoEfectivo(DateOnly hoy) => EstaVencida(hoy) ? EstadoVencida : Estado;

    private void ExigirVigente(string accion)
    {
        if (Estado != EstadoVigente)
            throw new CotizacionNoModificableException(
                $"No se puede {accion} una cotización {Estado.ToLowerInvariant()}.");
    }

    private void AplicarContenido(
        int? clienteId,
        string? nombreCliente,
        string? telefonoCliente,
        string? emailCliente,
        DateOnly fechaVencimiento,
        string? notas,
        IReadOnlyList<(int ProductoId, int Cantidad, decimal PrecioUnitario)> lineas,
        DateOnly hoy)
    {
        if (lineas is null || lineas.Count == 0)
            throw new ArgumentException("La cotización debe incluir al menos un producto.");

        if (lineas.Select(l => l.ProductoId).Distinct().Count() != lineas.Count)
            throw new ArgumentException("Un producto no puede repetirse en la misma cotización.");

        if (fechaVencimiento < hoy)
            throw new ArgumentException("El plazo de la cotización no puede estar en el pasado.");

        var notasLimpias = string.IsNullOrWhiteSpace(notas) ? null : notas.Trim();
        if (notasLimpias is { Length: > MaximoCaracteresNotas })
            throw new ArgumentException($"Las notas no pueden exceder {MaximoCaracteresNotas} caracteres.");

        ClienteId = clienteId;
        NombreCliente = Limpiar(nombreCliente);
        TelefonoCliente = Limpiar(telefonoCliente);
        EmailCliente = Limpiar(emailCliente);
        FechaVencimiento = fechaVencimiento;
        Notas = notasLimpias;

        _detalles.Clear();
        foreach (var (productoId, cantidad, precio) in lineas)
            _detalles.Add(DetalleCotizacion.Crear(productoId, cantidad, precio));

        Total = _detalles.Sum(d => d.Subtotal);
    }

    private static string? Limpiar(string? valor) => string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
