using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace StockManager.Infrastructure.RealTime;

/// <summary>
/// Hub de SignalR usado únicamente para "push" del servidor hacia el navegador (avisos de
/// stock cambiado); los clientes nunca invocan métodos de este Hub, así que no expone
/// ninguno. Requiere el mismo JWT que el resto de la API (ver la configuración de
/// JwtBearerEvents.OnMessageReceived en Program.cs, necesaria porque un WebSocket no puede
/// mandar el token en un header Authorization como una petición HTTP normal).
///
/// Incluye el rol "Cliente" (además de Admin/Empleado) porque también lo consume el catálogo
/// de la PWA: el stock de un producto ya es información pública ahí (se muestra sin login
/// especial en el catálogo), así que no hay nada que proteger de más al dejarlos conectarse.
/// </summary>
[Authorize(Roles = "Admin,Empleado,Cliente")]
public class StockHub : Hub
{
}
