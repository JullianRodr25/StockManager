using StockManager.Application.DTOs;

namespace StockManager.Application.Services;

/// <summary>
/// Notificaciones internas para el personal (la "campana" del panel). No confundir con
/// IWhatsAppSender/NotificacionLog, que son envíos externos a clientes/proveedores.
/// </summary>
public interface INotificacionInternaService
{
    Task<List<NotificacionInternaResponse>> ObtenerAsync(bool soloNoLeidas, int limite = 50);

    Task<int> ObtenerConteoNoLeidasAsync();

    Task<NotificacionInternaResponse> CrearAsync(string tipo, string titulo, string mensaje, string entidadTipo, int entidadId);

    Task MarcarLeidaAsync(int id);

    Task MarcarTodasLeidasAsync();
}
