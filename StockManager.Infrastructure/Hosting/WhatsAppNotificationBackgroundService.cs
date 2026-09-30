using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StockManager.Application.Services;
using StockManager.Domain.Entities;
using StockManager.Domain.Events;
using StockManager.Infrastructure.Data;
using StockManager.Infrastructure.Notificaciones;

namespace StockManager.Infrastructure.Hosting;

/// <summary>
/// Consume los DomainEvent publicados por IEventoNotificacionPublisher y hace el envío real
/// por WhatsApp. Corre durante toda la vida de la aplicación, leyendo del Channel en memoria;
/// cada evento se procesa en su propio scope de DI (AppDbContext e IWhatsAppSender son
/// Scoped, pero este BackgroundService es Singleton).
///
/// Cualquier error al procesar un evento se registra y NO detiene el loop: un fallo de
/// WhatsApp (Twilio caído, número inválido, etc.) nunca debe tumbar el proceso ni afectar
/// el procesamiento del resto de notificaciones pendientes en la cola.
/// </summary>
public class WhatsAppNotificationBackgroundService : BackgroundService
{
    private readonly Channel<DomainEvent> _canal;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<WhatsAppOptions> _opciones;
    private readonly ILogger<WhatsAppNotificationBackgroundService> _logger;

    public WhatsAppNotificationBackgroundService(
        Channel<DomainEvent> canal,
        IServiceProvider serviceProvider,
        IOptions<WhatsAppOptions> opciones,
        ILogger<WhatsAppNotificationBackgroundService> logger)
    {
        _canal = canal;
        _serviceProvider = serviceProvider;
        _opciones = opciones;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var evento in _canal.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcesarEventoAsync(evento, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error procesando evento de notificación {TipoEvento}", evento.GetType().Name);
            }
        }
    }

    private async Task ProcesarEventoAsync(DomainEvent evento, CancellationToken ct)
    {
        // Interruptor general: en un ambiente sin credenciales de Twilio configuradas (ej.
        // desarrollo local), los eventos simplemente se descartan sin intentar el envío.
        if (!_opciones.Value.Habilitado)
            return;

        using var scope = _serviceProvider.CreateScope();

        switch (evento)
        {
            case PedidoEstadoCambiadoEvent pedidoEvento:
                await ProcesarPedidoEstadoCambiadoAsync(pedidoEvento, scope.ServiceProvider, ct);
                break;

            case FacturaGeneradaEvent facturaEvento:
                await ProcesarFacturaGeneradaAsync(facturaEvento, scope.ServiceProvider, ct);
                break;

            case CuentaPorPagarProximaAVencerEvent cuentaEvento:
                await ProcesarCuentaPorPagarProximaAVencerAsync(cuentaEvento, scope.ServiceProvider, ct);
                break;

            // Otros eventos de dominio (ProductoVendidoEvent, ProductoRepuestoEvent) no
            // generan notificación por WhatsApp; se ignoran silenciosamente acá.
        }
    }

    private async Task ProcesarCuentaPorPagarProximaAVencerAsync(CuentaPorPagarProximaAVencerEvent evento, IServiceProvider sp, CancellationToken ct)
    {
        var telefonoAdmin = await ObtenerTelefonoAdminAsync(sp);
        if (string.IsNullOrWhiteSpace(telefonoAdmin))
            return;

        var db = sp.GetRequiredService<AppDbContext>();
        var sender = sp.GetRequiredService<IWhatsAppSender>();

        var cuenta = await db.CuentasPorPagar.AsNoTracking().FirstOrDefaultAsync(c => c.Id == evento.CuentaPorPagarId, ct);
        if (cuenta is null)
            return;

        var proveedor = await db.Proveedores.AsNoTracking().FirstOrDefaultAsync(p => p.Id == cuenta.ProveedorId, ct);
        if (proveedor is null)
            return;

        var totalAbonado = await db.AbonosCuentaPorPagar
            .AsNoTracking()
            .Where(a => a.CuentaPorPagarId == cuenta.Id)
            .SumAsync(a => (decimal?)a.Monto, ct) ?? 0m;

        var mensaje = PlantillasMensajesWhatsApp.CuentaPorPagarProximaAVencer(
            proveedor.Nombre, cuenta.Concepto, cuenta.MontoTotal, cuenta.MontoTotal - totalAbonado, cuenta.FechaVencimiento);

        var resultado = await sender.EnviarTextoAsync(telefonoAdmin, mensaje);
        await RegistrarLogAsync(db, telefonoAdmin, "CuentaPorPagar", cuenta.Id, resultado, ct);
    }

    private async Task ProcesarPedidoEstadoCambiadoAsync(PedidoEstadoCambiadoEvent evento, IServiceProvider sp, CancellationToken ct)
    {
        var db = sp.GetRequiredService<AppDbContext>();
        var sender = sp.GetRequiredService<IWhatsAppSender>();

        var pedido = await db.Pedidos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == evento.PedidoId, ct);
        if (pedido is null)
            return;

        var cliente = await db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == pedido.ClienteId, ct);
        if (cliente is null)
            return;

        var mensajeCliente = PlantillasMensajesWhatsApp.PedidoParaCliente(cliente.Nombre, pedido.Id, evento.NuevoEstado);
        var resultadoCliente = await sender.EnviarTextoAsync(cliente.Telefono, mensajeCliente);
        await RegistrarLogAsync(db, cliente.Telefono, "Pedido", pedido.Id, resultadoCliente, ct);

        // Pedido recién creado (queda en "Pendiente"): avisar también a la tienda, si hay
        // un teléfono de administración configurado.
        if (evento.NuevoEstado == "Pendiente")
        {
            var telefonoAdmin = await ObtenerTelefonoAdminAsync(sp);
            if (!string.IsNullOrWhiteSpace(telefonoAdmin))
            {
                var mensajeAdmin = PlantillasMensajesWhatsApp.PedidoNuevoParaAdmin(pedido.Id, cliente.Nombre, pedido.Total);
                var resultadoAdmin = await sender.EnviarTextoAsync(telefonoAdmin, mensajeAdmin);
                await RegistrarLogAsync(db, telefonoAdmin, "Pedido", pedido.Id, resultadoAdmin, ct);
            }
        }
    }

    /// <summary>
    /// El teléfono de notificaciones administrativas vive en Configuracion (base de datos),
    /// no en WhatsAppOptions (appsettings.json), para que un Admin pueda cambiarlo desde la
    /// pantalla de Configuración sin necesitar un despliegue. Se resuelve por evento (en vez
    /// de cachearlo) porque el volumen de eventos es bajo y así siempre se usa el valor vigente.
    /// </summary>
    private static async Task<string?> ObtenerTelefonoAdminAsync(IServiceProvider sp)
    {
        var configuracionService = sp.GetRequiredService<IConfiguracionService>();
        var configuracion = await configuracionService.ObtenerAsync();
        return configuracion.TelefonoNotificacionesAdmin;
    }

    private async Task ProcesarFacturaGeneradaAsync(FacturaGeneradaEvent evento, IServiceProvider sp, CancellationToken ct)
    {
        var db = sp.GetRequiredService<AppDbContext>();
        var sender = sp.GetRequiredService<IWhatsAppSender>();
        var tokenService = sp.GetRequiredService<IFacturaLinkTokenService>();

        var factura = await db.Facturas.AsNoTracking().FirstOrDefaultAsync(f => f.Id == evento.FacturaId, ct);
        if (factura is null || factura.Numero is null)
            return;

        string? nombreCliente = null;
        string? telefono = null;

        if (factura.VentaId.HasValue)
        {
            var venta = await db.Ventas.AsNoTracking().FirstOrDefaultAsync(v => v.Id == factura.VentaId.Value, ct);
            if (venta is null)
                return;

            if (venta.ClienteId.HasValue)
            {
                var cliente = await db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == venta.ClienteId.Value, ct);
                nombreCliente = cliente?.Nombre;
                telefono = cliente?.Telefono;
            }

            // Venta de mostrador sin cliente registrado: se usan los datos del comprador ocasional.
            nombreCliente ??= venta.NombreComprador;
            telefono ??= venta.TelefonoComprador;
        }
        else if (factura.PedidoId.HasValue)
        {
            var pedido = await db.Pedidos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == factura.PedidoId.Value, ct);
            if (pedido is null)
                return;

            var cliente = await db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == pedido.ClienteId, ct);
            nombreCliente = cliente?.Nombre;
            telefono = cliente?.Telefono;
        }

        if (string.IsNullOrWhiteSpace(telefono))
            return; // Sin un teléfono a quién enviarle, no hay nada que hacer.

        var token = tokenService.GenerarToken(factura.Id, TimeSpan.FromMinutes(_opciones.Value.FacturaLinkVigenciaMinutos));
        var urlPdf = $"{_opciones.Value.PublicBaseUrl.TrimEnd('/')}/api/facturas/{factura.Id}/pdf?t={Uri.EscapeDataString(token)}";

        var mensaje = PlantillasMensajesWhatsApp.FacturaParaCliente(nombreCliente ?? "cliente", factura.Numero!);
        var resultado = await sender.EnviarDocumentoAsync(telefono!, mensaje, urlPdf);

        var referenciaTipo = factura.VentaId.HasValue ? "Venta" : "Pedido";
        var referenciaId = factura.VentaId ?? factura.PedidoId!.Value;
        await RegistrarLogAsync(db, telefono!, referenciaTipo, referenciaId, resultado, ct);
    }

    private static async Task RegistrarLogAsync(
        AppDbContext db,
        string destinatario,
        string referenciaTipo,
        int referenciaId,
        ResultadoEnvioWhatsApp resultado,
        CancellationToken ct)
    {
        var log = NotificacionLog.Crear(
            canal: "WhatsApp",
            destinatario: destinatario,
            referenciaTipo: referenciaTipo,
            referenciaId: referenciaId,
            estado: resultado.Exitoso ? "Enviado" : "Fallido",
            detalleError: resultado.Error);

        db.NotificacionesLog.Add(log);
        await db.SaveChangesAsync(ct);
    }
}
