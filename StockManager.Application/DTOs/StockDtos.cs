namespace StockManager.Application.DTOs;

/// <summary>
/// Un cambio de stock puntual, tal como se transmite a los clientes conectados en tiempo
/// real: solo lo mínimo para que el frontend actualice el número en pantalla sin tener que
/// volver a pedir el producto completo.
/// </summary>
public record CambioStockDto(int ProductoId, int StockActual);
