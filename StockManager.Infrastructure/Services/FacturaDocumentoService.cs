using Microsoft.EntityFrameworkCore;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Entities;
using StockManager.Infrastructure.Data;

namespace StockManager.Infrastructure.Services;

/// <summary>
/// Arma el documento de factura (ver <see cref="FacturaDocumentoResponse"/>) en un único
/// lugar. Tiquete térmico, vista digital y PDF solo lo presentan; ninguna decide por su
/// cuenta quién es "cliente final" ni cómo se agrupa el IVA.
/// </summary>
public class FacturaDocumentoService : IFacturaDocumentoService
{
    private const string NombreClienteFinal = "CLIENTE FINAL";

    private readonly AppDbContext _db;

    // Línea ya normalizada, común a Venta y Pedido.
    private sealed record LineaOrigen(string Producto, int Cantidad, decimal PrecioUnitario, decimal TarifaIva);

    public FacturaDocumentoService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<FacturaDocumentoResponse?> ObtenerPorFacturaIdAsync(int facturaId)
    {
        var factura = await _db.Facturas.AsNoTracking().FirstOrDefaultAsync(f => f.Id == facturaId);
        return factura is null ? null : await ConstruirAsync(factura);
    }

    public async Task<FacturaDocumentoResponse?> ObtenerPorVentaIdAsync(int ventaId)
    {
        var factura = await _db.Facturas.AsNoTracking().FirstOrDefaultAsync(f => f.VentaId == ventaId);
        return factura is null ? null : await ConstruirAsync(factura);
    }

    private async Task<FacturaDocumentoResponse?> ConstruirAsync(Factura factura)
    {
        if (factura.Numero is null)
            return null;

        var configuracion = await _db.Configuraciones.AsNoTracking().FirstOrDefaultAsync();
        var emisor = ConstruirEmisor(configuracion);

        if (factura.VentaId.HasValue)
            return await ConstruirDeVentaAsync(factura, factura.VentaId.Value, emisor, configuracion);

        if (factura.PedidoId.HasValue)
            return await ConstruirDePedidoAsync(factura, factura.PedidoId.Value, emisor, configuracion);

        return null; // Factura exige exactamente una de las dos referencias; no debería pasar.
    }

    private async Task<FacturaDocumentoResponse?> ConstruirDeVentaAsync(
        Factura factura, int ventaId, FacturaEmisorDto emisor, Configuracion? configuracion)
    {
        var venta = await _db.Ventas.AsNoTracking().FirstOrDefaultAsync(v => v.Id == ventaId);
        if (venta is null)
            return null;

        var vendedor = await _db.Empleados.AsNoTracking()
            .Where(e => e.Id == venta.EmpleadoId)
            .Select(e => e.Nombre)
            .FirstOrDefaultAsync();

        Cliente? cliente = null;
        if (venta.ClienteId.HasValue)
            cliente = await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == venta.ClienteId.Value);

        var lineas = await _db.DetallesVenta.AsNoTracking()
            .Where(d => d.VentaId == ventaId)
            .Join(_db.Productos.AsNoTracking(), d => d.ProductoId, p => p.Id,
                (d, p) => new LineaOrigen(p.Nombre, d.Cantidad, d.PrecioUnitario, p.TarifaIva))
            .ToListAsync();

        var comprador = ConstruirCompradorDeVenta(venta, cliente);
        var pagos = await ObtenerPagosAsync(venta);

        return Ensamblar(factura, vendedor, emisor, comprador, lineas, pagos,
            venta.MontoRecibido, venta.Cambio, configuracion);
    }

    private async Task<FacturaDocumentoResponse?> ConstruirDePedidoAsync(
        Factura factura, int pedidoId, FacturaEmisorDto emisor, Configuracion? configuracion)
    {
        var pedido = await _db.Pedidos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == pedidoId);
        if (pedido is null)
            return null;

        var cliente = await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == pedido.ClienteId);

        var lineas = await _db.DetallesPedido.AsNoTracking()
            .Where(d => d.PedidoId == pedidoId)
            .Join(_db.Productos.AsNoTracking(), d => d.ProductoId, p => p.Id,
                (d, p) => new LineaOrigen(p.Nombre, d.Cantidad, d.PrecioUnitario, p.TarifaIva))
            .ToListAsync();

        var comprador = ConstruirCompradorRegistrado(cliente, pedido.ClienteId, direccionEntrega: pedido.Direccion);

        // Un pedido de la PWA no registra método de pago en el sistema: no se inventa uno.
        return Ensamblar(factura, vendedor: null, emisor, comprador, lineas,
            new List<DetallePagoResponse>(), null, null, configuracion);
    }

    // ---------- Comprador ----------

    private static FacturaCompradorDto ConstruirCompradorDeVenta(Venta venta, Cliente? cliente)
    {
        // Snapshot fiscal: si el comprador pidió factura electrónica, esos datos (congelados
        // al momento de la venta) mandan sobre lo que hoy diga el perfil del cliente.
        if (venta.RequiereFacturaElectronica && !string.IsNullOrWhiteSpace(venta.NumeroDocumentoFacturado))
        {
            var direccion = Limpiar(venta.DireccionFacturada) ?? Limpiar(cliente?.Direccion);
            return new FacturaCompradorDto(
                venta.ClienteId,
                Limpiar(venta.RazonSocialFacturada) ?? cliente?.Nombre ?? NombreClienteFinal,
                Limpiar(venta.TipoDocumentoFacturado),
                venta.NumeroDocumentoFacturado!.Trim(),
                direccion,
                cliente?.Telefono ?? Limpiar(venta.TelefonoComprador),
                null,
                EsClienteFinal: false,
                FaltaDocumento: false,
                FaltaDireccion: direccion is null);
        }

        // Sin cliente registrado: "Cliente final", sin cédula y sin guardar nada del comprador.
        if (cliente is null)
            return ClienteFinal();

        return ConstruirCompradorRegistrado(cliente, venta.ClienteId, direccionEntrega: null);
    }

    private static FacturaCompradorDto ConstruirCompradorRegistrado(Cliente? cliente, int? clienteId, string? direccionEntrega)
    {
        if (cliente is null)
            return ClienteFinal();

        var documento = Limpiar(cliente.NumeroIdentificacion);
        var direccion = Limpiar(cliente.Direccion);

        return new FacturaCompradorDto(
            clienteId,
            cliente.Nombre,
            documento is null ? null : "C.C.",
            documento,
            direccion,
            Limpiar(cliente.Telefono),
            Limpiar(direccionEntrega),
            EsClienteFinal: false,
            FaltaDocumento: documento is null,
            FaltaDireccion: direccion is null);
    }

    private static FacturaCompradorDto ClienteFinal() =>
        new(null, NombreClienteFinal, null, null, null, null, null,
            EsClienteFinal: true, FaltaDocumento: false, FaltaDireccion: false);

    // ---------- Pagos ----------

    private async Task<List<DetallePagoResponse>> ObtenerPagosAsync(Venta venta)
    {
        if (string.IsNullOrWhiteSpace(venta.MetodoPago))
            return new List<DetallePagoResponse>();

        if (venta.MetodoPago != "Mixto")
            return new List<DetallePagoResponse> { new(venta.MetodoPago, venta.Total) };

        var directos = await _db.DetallesPagoVenta.AsNoTracking()
            .Where(d => d.VentaId == venta.Id)
            .Select(d => new DetallePagoResponse(d.MetodoPago, d.Monto))
            .ToListAsync();
        if (directos.Count > 0)
            return directos;

        // Cuenta fiada cerrada como "Mixto": el desglose sale de los abonos ya registrados.
        return await _db.AbonosCuenta.AsNoTracking()
            .Where(a => a.VentaId == venta.Id)
            .GroupBy(a => a.MetodoPago)
            .Select(g => new DetallePagoResponse(g.Key, g.Sum(a => a.Monto)))
            .ToListAsync();
    }

    // ---------- Emisor ----------

    private static FacturaEmisorDto ConstruirEmisor(Configuracion? c)
    {
        if (c is null)
            return new FacturaEmisorDto(null, null, null, null, null, null, null, null, null, null);

        return new FacturaEmisorDto(
            Limpiar(c.NombreEmpresa),
            Limpiar(c.NitEmpresa),
            Limpiar(c.DireccionEmpresa),
            Limpiar(c.BarrioEmpresa),
            Limpiar(c.CiudadEmpresa),
            Limpiar(c.TelefonoEmpresa),
            Limpiar(c.EmailEmpresa),
            Limpiar(c.ResponsabilidadIvaEmpresa),
            Limpiar(c.ActividadEconomicaEmpresa),
            ArmarTextoResolucion(c));
    }

    /// <summary>
    /// "Resolución DIAN No. X del dd/MM/yyyy, prefijo P, del 1 al 5000, vigencia 12 meses".
    /// Solo incluye las partes que Gold configuró; sin número de resolución no hay texto.
    /// </summary>
    private static string? ArmarTextoResolucion(Configuracion c)
    {
        if (string.IsNullOrWhiteSpace(c.ResolucionDianNumero))
            return null;

        var partes = new List<string>();
        var encabezado = $"Resolución DIAN No. {c.ResolucionDianNumero.Trim()}";
        if (c.ResolucionDianFecha.HasValue)
            encabezado += $" del {c.ResolucionDianFecha.Value:dd/MM/yyyy}";
        partes.Add(encabezado);

        if (!string.IsNullOrWhiteSpace(c.ResolucionDianPrefijo))
            partes.Add($"prefijo {c.ResolucionDianPrefijo.Trim()}");
        if (c.ResolucionDianRangoDesde.HasValue && c.ResolucionDianRangoHasta.HasValue)
            partes.Add($"del {c.ResolucionDianRangoDesde} al {c.ResolucionDianRangoHasta}");
        if (c.ResolucionDianVigenciaMeses.HasValue)
            partes.Add($"vigencia {c.ResolucionDianVigenciaMeses} meses");

        return string.Join(", ", partes);
    }

    // ---------- Totales ----------

    private static FacturaDocumentoResponse Ensamblar(
        Factura factura,
        string? vendedor,
        FacturaEmisorDto emisor,
        FacturaCompradorDto comprador,
        List<LineaOrigen> origen,
        List<DetallePagoResponse> pagos,
        decimal? montoRecibido,
        decimal? cambio,
        Configuracion? configuracion)
    {
        var lineas = origen
            .Select(l => new FacturaLineaDto(l.Producto, l.Cantidad, l.PrecioUnitario, l.TarifaIva, l.PrecioUnitario * l.Cantidad))
            .ToList();

        // Misma convención que VentaService: PrecioUnitario es el valor cobrado y el IVA de la
        // línea se calcula como bruto * tarifa; la base es lo que queda. Así la factura cuadra
        // con lo que ya muestran el resto de pantallas.
        var resumenIva = lineas
            .GroupBy(l => l.TarifaIva)
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var bruto = g.Sum(l => l.Total);
                var iva = g.Sum(l => l.Total * (l.TarifaIva / 100m));
                return new FacturaResumenIvaDto(g.Key, bruto - iva, iva);
            })
            .ToList();

        return new FacturaDocumentoResponse(
            factura.Numero!,
            factura.Fecha,
            vendedor,
            emisor,
            comprador,
            lineas,
            resumenIva,
            resumenIva.Sum(r => r.Base),
            resumenIva.Sum(r => r.Iva),
            factura.Total,
            pagos,
            montoRecibido,
            cambio,
            Limpiar(configuracion?.TextoLegalFactura),
            Limpiar(configuracion?.PoliticaCambiosFactura));
    }

    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();
}
