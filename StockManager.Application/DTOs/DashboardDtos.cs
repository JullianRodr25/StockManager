namespace StockManager.Application.DTOs;

/// <summary>
/// Resumen agregado para el Dashboard del panel. Se calcula en una sola pasada por request
/// (sin caché) porque el volumen de datos de esta app es pequeño y el Dashboard no se visita
/// con una frecuencia que justifique la complejidad de cachear.
/// </summary>
public record DashboardResumenResponse(
    decimal VentasHoyTotal,
    int VentasHoyCantidad,
    int ProductosStockBajo,
    int PedidosActivos,
    int CuentasPorPagarPorVencer,
    decimal TotalPorPagarProveedores,
    decimal TotalPorCobrarFiado,
    int ClientesConFiadoAbierto,
    int ClientesActivos,
    List<ActividadRecienteResponse> ActividadReciente);

public record ActividadRecienteResponse(string Descripcion, DateTime Fecha);
