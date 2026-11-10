using System.ComponentModel.DataAnnotations;

namespace StockManager.Application.DTOs;

public class RegistrarRetiroRequest
{
    /// <summary>Efectivo que se saca de la caja; debe ser mayor a 0.</summary>
    [Range(typeof(decimal), "0.01", "999999999", ErrorMessage = "El monto del retiro debe ser mayor a 0.")]
    public decimal Monto { get; set; }
}

public record RetiroCajaResponse(
    int Id,
    int TurnoCajaId,
    int EmpleadoId,
    string EmpleadoNombre,
    DateTime Fecha,
    decimal Monto,
    bool Anulado,
    string? AnuladoPorNombre,
    DateTime? FechaAnulacion);

/// <summary>Total retirado (sin anulados) en un día calendario de Colombia.</summary>
public record RetirosPorDiaResponse(DateOnly Fecha, int Cantidad, decimal Total);

public record ReporteRetirosResponse(
    DateOnly Desde,
    DateOnly Hasta,
    decimal TotalRetirado,
    int CantidadRetiros,
    List<RetirosPorDiaResponse> PorDia,
    List<RetiroCajaResponse> Retiros);
