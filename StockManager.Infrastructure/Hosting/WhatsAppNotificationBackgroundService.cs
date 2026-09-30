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
    private readonly IOptions<TwilioOptions> _opcionesTwilio;
    private readonly ILogger<WhatsAppNotificationBackgroundService> _logger;

    public WhatsAppNotificationBackgroundService(
        Channel<DomainEvent> canal,
        IServiceProvider serviceProvider,
        IOptions<WhatsAppOptions> opciones,
        IOptions<TwilioOptions> opcionesTwilio,
        ILogger<WhatsAppNotificationBackgroundService> logger)
    {
        _canal = canal;
        _serviceProvider = serviceProvider;
        _opciones = opciones;
        _opcionesTwilio = opcionesTwilio;
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
        using var scope = _serviceProvider.CreateScope();

        // Las notificaciones internas (campana del panel) son independientes de si el envío
        // por WhatsApp está habilitado — nunca deben quedar apagadas solo porque WhatsApp lo
        // está (ver GenerarNotificacionInternaSiAplicaAsync).
        await GenerarNotificacionInternaSiAplicaAsync(evento, scope.ServiceProvider, ct);

        // Interruptor general de WhatsApp: en un ambiente sin credenciales de Twilio
        // configuradas (ej. desarrollo local), los envíos reales simplemente se descartan.
        if (!_opciones.Value.Habilitado)
            return;

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

            case StockBajoProveedorEvent stockBajoEvento:
                await ProcesarStockBajoProveedorAsync(stockBajoEvento, scope.ServiceProvider, ct);
                break;

            // Otros eventos de dominio (ProductoVendidoEvent, ProductoRepuestoEvent) no
            // generan notificación por WhatsApp; se ignoran silenciosamente acá.
        }
    }

    /// <summary>
    /// Genera la notificación interna correspondiente (campana del panel) para los eventos
    /// que le interesan al personal, sin importar si el envío por WhatsApp está habilitado.
    /// Los otros eventos (venta/reposición de stock, factura generada, stock bajo a
    /// proveedor) no generan campana — ya sea porque no son accionables por el empleado
    /// (factura) o porque el chequeo de stock bajo (NotificacionesStockBajoCheckService) ya
    /// cubre esa alerta de forma general.
    /// </summary>
    private async Task GenerarNotificacionInternaSiAplicaAsync(DomainEvent evento, IServiceProvider sp, CancellationToken ct)
    {
        var notificaciones = sp.GetRequiredService<INotificacionInternaService>();

        switch (evento)
        {
            case PedidoEstadoCambiadoEvent pedidoEvento when pedidoEvento.NuevoEstado == "Pendiente":
                var db = sp.GetRequiredService<AppDbContext>();
                var pedido = await db.Pedidos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == pedidoEvento.PedidoId, ct);
                if (pedido is not null)
                {
                    var cliente = await db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == pedido.ClienteId, ct);
                    await notificaciones.CrearAsync(
                        "PedidoNuevo",
                        $"Nuevo pedido #{pedido.Id}",
                        $"{cliente?.Nombre ?? "Un cliente"} hizo un pedido por ${pedido.Total:N0}.",
                        "Pedido",
                        pedido.Id);
                }
                break;

            case CuentaPorPagarProximaAVencerEvent cuentaEvento:
                var db2 = sp.GetRequiredService<AppDbContext>();
                var cuenta = await db2.CuentasPorPagar.AsNoTracking().FirstOrDefaultAsync(c => c.Id == cuentaEvento.CuentaPorPagarId, ct);
                if (cuenta is not null)
                {
                    var proveedor = await db2.Proveedores.AsNoTracking().FirstOrDefaultAsync(p => p.Id == cuenta.ProveedorId, ct);
                    var vencida = cuenta.FechaVencimiento.Date < DateTime.UtcNow.Date;
                    await notificaciones.CrearAsync(
                        "CuentaPorPagarProximaAVencer",
                        vencida ? "Cuenta por pagar vencida" : "Cuenta por pagar próxima a vencer",
                        $"{proveedor?.Nombre ?? "Un proveedor"} — {cuenta.Concepto}, vence el {cuenta.FechaVencimiento:dd/MM/yyyy}.",
                        "CuentaPorPagar",
                        cuenta.Id);
                }
                break;
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

        if (!IntentarObtenerContentSid(_opcionesTwilio.Value.ContentSidAlertaCuentaPorPagar, out var contentSid))
        {
            await RegistrarPlantillaNoConfiguradaAsync(db, telefonoAdmin, "CuentaPorPagar", cuenta.Id, "alerta_cuenta_por_pagar", ct);
            return;
        }

        var totalAbonado = await db.AbonosCuentaPorPagar
            .AsNoTracking()
            .Where(a => a.CuentaPorPagarId == cuenta.Id)
            .SumAsync(a => (decimal?)a.Monto, ct) ?? 0m;

        var variables = PlantillasMensajesWhatsApp.CuentaPorPagarProximaAVencer(
            proveedor.Nombre, cuenta.Concepto, cuenta.MontoTotal, cuenta.MontoTotal - totalAbonado, cuenta.FechaVencimiento);

        var resultado = await sender.EnviarPlantillaAsync(telefonoAdmin, contentSid, variables);
        await RegistrarLogAsync(db, telefonoAdmin, "CuentaPorPagar", cuenta.Id, resultado, ct);
    }

    /// <summary>
    /// Avisa a un proveedor (en un único mensaje) sobre todos los productos que le compramos
    /// y que están actualmente en stock bajo. Se re-consulta la lista de productos en vez de
    /// reutilizar la que vio el chequeo periódico, porque entre que se publicó el evento y se
    /// procesa puede haber pasado algo de tiempo (y el volumen de eventos es bajo).
    /// </summary>
    private async Task ProcesarStockBajoProveedorAsync(StockBajoProveedorEvent evento, IServiceProvider sp, CancellationToken ct)
    {
        var db = sp.GetRequiredService<AppDbContext>();
        var sender = sp.GetRequiredService<IWhatsAppSender>();

        var proveedor = await db.Proveedores.AsNoTracking().FirstOrDefaultAsync(p => p.Id == evento.ProveedorId, ct);
        if (proveedor is null || string.IsNullOrWhiteSpace(proveedor.NumeroWhatsApp))
            return;

        if (!IntentarObtenerContentSid(_opcionesTwilio.Value.ContentSidAlertaStockBajoProveedor, out var contentSid))
        {
            await RegistrarPlantillaNoConfiguradaAsync(db, proveedor.NumeroWhatsApp!, "StockBajoProveedor", proveedor.Id, "alerta_stock_bajo_proveedor", ct);
            return;
        }

        var productos = await db.Productos.AsNoTracking()
            .Where(p => p.ProveedorId == evento.ProveedorId && p.Activo && p.StockActual <= p.StockMinimo)
            .Select(p => new { p.Nombre, p.StockActual, p.StockMinimo })
            .ToListAsync(ct);

        if (productos.Count == 0)
            return; // Se repuso el stock entre que se publicó el evento y se procesó.

        var variables = PlantillasMensajesWhatsApp.StockBajoProveedor(
            proveedor.Nombre,
            productos.Select(p => (p.Nombre, p.StockActual, p.StockMinimo)).ToList());

        var resultado = await sender.EnviarPlantillaAsync(proveedor.NumeroWhatsApp!, contentSid, variables);
        await RegistrarLogAsync(db, proveedor.NumeroWhatsApp!, "StockBajoProveedor", proveedor.Id, resultado, ct);
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

        if (IntentarObtenerContentSid(_opcionesTwilio.Value.ContentSidPedidoActualizacionCliente, out var contentSidCliente))
        {
            var variablesCliente = PlantillasMensajesWhatsApp.PedidoParaCliente(cliente.Nombre, pedido.Id, evento.NuevoEstado);
            var resultadoCliente = await sender.EnviarPlantillaAsync(cliente.Telefono, contentSidCliente, variablesCliente);
            await RegistrarLogAsync(db, cliente.Telefono, "Pedido", pedido.Id, resultadoCliente, ct);
        }
        else
        {
            await RegistrarPlantillaNoConfiguradaAsync(db, cliente.Telefono, "Pedido", pedido.Id, "pedido_actualizacion_cliente", ct);
        }

        // Pedido recién creado (queda en "Pendiente"): avisar también a la tienda, si hay
        // un teléfono de administración configurado.
        if (evento.NuevoEstado == "Pendiente")
        {
            var telefonoAdmin = await ObtenerTelefonoAdminAsync(sp);
            if (!string.IsNullOrWhiteSpace(telefonoAdmin))
            {
                if (IntentarObtenerContentSid(_opcionesTwilio.Value.ContentSidPedidoNuevoAdmin, out var contentSidAdmin))
                {
                    var variablesAdmin = PlantillasMensajesWhatsApp.PedidoNuevoParaAdmin(pedido.Id, cliente.Nombre, pedido.Total);
                    var resultadoAdmin = await sender.EnviarPlantillaAsync(telefonoAdmin, contentSidAdmin, variablesAdmin);
                    await RegistrarLogAsync(db, telefonoAdmin, "Pedido", pedido.Id, resultadoAdmin, ct);
                }
                else
                {
                    await RegistrarPlantillaNoConfiguradaAsync(db, telefonoAdmin, "Pedido", pedido.Id, "pedido_nuevo_admin", ct);
                }
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

        var referenciaTipo = factura.VentaId.HasValue ? "Venta" : "Pedido";
        var referenciaId = factura.VentaId ?? factura.PedidoId!.Value;

        if (!IntentarObtenerContentSid(_opcionesTwilio.Value.ContentSidFacturaCliente, out var contentSid))
        {
            await RegistrarPlantillaNoConfiguradaAsync(db, telefono!, referenciaTipo, referenciaId, "factura_cliente", ct);
            return;
        }

        var token = tokenService.GenerarToken(factura.Id, TimeSpan.FromMinutes(_opciones.Value.FacturaLinkVigenciaMinutos));
        var urlPdf = $"{_opciones.Value.PublicBaseUrl.TrimEnd('/')}/api/facturas/{factura.Id}/pdf?t={Uri.EscapeDataString(token)}";

        var variables = PlantillasMensajesWhatsApp.FacturaParaCliente(nombreCliente ?? "cliente", factura.Numero!, urlPdf);
        var resultado = await sender.EnviarPlantillaAsync(telefono!, contentSid, variables);

        await RegistrarLogAsync(db, telefono!, referenciaTipo, referenciaId, resultado, ct);
    }

    /// <summary>
    /// true y da el ContentSid listo para usar si la plantilla ya fue aprobada y configurada;
    /// false si TwilioOptions todavía tiene ese campo vacío (plantilla pendiente de aprobación
    /// en Meta o simplemente no configurada aún).
    /// </summary>
    private static bool IntentarObtenerContentSid(string? contentSidConfigurado, out string contentSid)
    {
        if (string.IsNullOrWhiteSpace(contentSidConfigurado))
        {
            contentSid = string.Empty;
            return false;
        }

        contentSid = contentSidConfigurado;
        return true;
    }

    /// <summary>
    /// Deja constancia en NotificacionLog de que un mensaje no se envió porque su plantilla de
    /// WhatsApp todavía no está configurada — nunca se intenta mandar como texto libre en su
    /// lugar, porque fuera de la ventana de 24h de una conversación Meta lo rechazaría (o
    /// podría penalizar el número). Ver TwilioOptions.ContentSidXxx.
    /// </summary>
    private static async Task RegistrarPlantillaNoConfiguradaAsync(
        AppDbContext db,
        string destinatario,
        string referenciaTipo,
        int referenciaId,
        string nombrePlantilla,
        CancellationToken ct)
    {
        var log = NotificacionLog.Crear(
            canal: "WhatsApp",
            destinatario: destinatario,
            referenciaTipo: referenciaTipo,
            referenciaId: referenciaId,
            estado: "Fallido",
            detalleError: $"Plantilla '{nombrePlantilla}' no configurada (ContentSid vacío).");

        db.NotificacionesLog.Add(log);
        await db.SaveChangesAsync(ct);
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
