using Microsoft.AspNetCore.SignalR;
using StockManager.Application.DTOs;
using StockManager.Application.Services;

namespace StockManager.Infrastructure.RealTime;

/// <summary>
/// Implementación de IStockNotificador sobre SignalR. Manda el mensaje "StockActualizado" a
/// todos los clientes conectados al Hub — no hay grupos ni filtrado por sucursal porque hoy
/// el sistema es de una sola tienda (ver la investigación previa a esta feature); si en el
/// futuro hay más de una sucursal, este es el lugar para mandar solo al grupo correspondiente.
/// </summary>
public class SignalRStockNotificador : IStockNotificador
{
    private readonly IHubContext<StockHub> _hubContext;

    public SignalRStockNotificador(IHubContext<StockHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotificarCambiosAsync(IReadOnlyCollection<CambioStockDto> cambios)
    {
        if (cambios.Count == 0) return;

        await _hubContext.Clients.All.SendAsync("StockActualizado", cambios);
    }
}
