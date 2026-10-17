using StockManager.Domain.Exceptions;

namespace StockManager.Domain.Entities;

/// <summary>
/// Entidad TurnoCaja del dominio.
/// Es el periodo en que un empleado maneja la caja: se abre con una base de efectivo, recibe
/// pagos (ventas, abonos de fiado) y hace pagos (a proveedores), y se cierra contando el
/// efectivo físico contra lo que el sistema esperaba.
///
/// Un turno cerrado es inmutable: guarda una "foto" de los totales al momento del cierre, de
/// modo que corregir después una venta o un precio no altere un cierre que ya se firmó.
/// </summary>
public class TurnoCaja
{
    public const string EstadoAbierto = "Abierto";
    public const string EstadoCerrado = "Cerrado";

    public int Id { get; private set; }
    public int EmpleadoId { get; private set; }
    public DateTime FechaApertura { get; private set; }
    public decimal BaseInicial { get; private set; }
    public string Estado { get; private set; } = EstadoAbierto;

    // --- Datos del cierre: solo tienen valor cuando Estado == Cerrado.
    public DateTime? FechaCierre { get; private set; }

    /// <summary>Quién cerró: normalmente el mismo empleado; distinto si un Admin forzó el cierre.</summary>
    public int? CerradoPorEmpleadoId { get; private set; }

    public decimal? EfectivoContado { get; private set; }
    public decimal? EfectivoEsperado { get; private set; }
    public decimal? Diferencia { get; private set; }
    public string? NotaCierre { get; private set; }

    /// <summary>Desglose del conteo por denominación (JSON), para reimprimir el cierre fielmente.</summary>
    public string? DetalleConteo { get; private set; }

    public decimal? TotalVentasEfectivo { get; private set; }
    public decimal? TotalAbonosFiadoEfectivo { get; private set; }
    public decimal? TotalPagosProveedorEfectivo { get; private set; }
    public decimal? TotalTransferencias { get; private set; }
    public decimal? TotalTarjeta { get; private set; }

    private TurnoCaja() { }

    public static TurnoCaja Abrir(int empleadoId, decimal baseInicial)
    {
        if (empleadoId <= 0)
            throw new ArgumentException("EmpleadoId debe ser mayor a 0.", nameof(empleadoId));

        if (baseInicial < 0)
            throw new ArgumentException("La base inicial no puede ser negativa.", nameof(baseInicial));

        return new TurnoCaja
        {
            EmpleadoId = empleadoId,
            FechaApertura = DateTime.UtcNow,
            BaseInicial = baseInicial,
            Estado = EstadoAbierto
        };
    }

    /// <summary>
    /// Cierra el turno con el efectivo realmente contado. Si hay diferencia (sobrante o
    /// faltante) exige una nota que la explique: así ninguna diferencia queda sin justificar.
    /// </summary>
    public void Cerrar(
        decimal efectivoContado,
        string? nota,
        TotalesTurnoCaja totales,
        int cerradoPorEmpleadoId,
        string? detalleConteo = null)
    {
        if (Estado != EstadoAbierto)
            throw new TurnoCajaEstadoInvalidoException(Id, Estado, EstadoAbierto);

        if (efectivoContado < 0)
            throw new OperacionInvalidaCajaException("El efectivo contado no puede ser negativo.");

        if (cerradoPorEmpleadoId <= 0)
            throw new ArgumentException("CerradoPorEmpleadoId debe ser mayor a 0.", nameof(cerradoPorEmpleadoId));

        var esperado = totales.EfectivoEsperado(BaseInicial);
        var diferencia = efectivoContado - esperado;
        var notaLimpia = string.IsNullOrWhiteSpace(nota) ? null : nota.Trim();

        if (diferencia != 0 && notaLimpia is null)
            throw new OperacionInvalidaCajaException(
                "Hay una diferencia entre lo contado y lo esperado: escribe una nota que la explique.");

        if (notaLimpia is { Length: > 300 })
            throw new OperacionInvalidaCajaException("La nota no puede exceder 300 caracteres.");

        FechaCierre = DateTime.UtcNow;
        CerradoPorEmpleadoId = cerradoPorEmpleadoId;
        EfectivoContado = efectivoContado;
        EfectivoEsperado = esperado;
        Diferencia = diferencia;
        NotaCierre = notaLimpia;
        DetalleConteo = detalleConteo;
        TotalVentasEfectivo = totales.VentasEfectivo;
        TotalAbonosFiadoEfectivo = totales.AbonosFiadoEfectivo;
        TotalPagosProveedorEfectivo = totales.PagosProveedorEfectivo;
        TotalTransferencias = totales.Transferencias;
        TotalTarjeta = totales.Tarjeta;
        Estado = EstadoCerrado;
    }

    /// <summary>Totales congelados al cierre; solo disponible con el turno cerrado.</summary>
    public TotalesTurnoCaja? TotalesCierre =>
        Estado == EstadoCerrado
            ? new TotalesTurnoCaja(
                TotalVentasEfectivo ?? 0m,
                TotalAbonosFiadoEfectivo ?? 0m,
                TotalPagosProveedorEfectivo ?? 0m,
                TotalTransferencias ?? 0m,
                TotalTarjeta ?? 0m)
            : null;
}
