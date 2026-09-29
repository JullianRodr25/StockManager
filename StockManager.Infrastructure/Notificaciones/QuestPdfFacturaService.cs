using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StockManager.Application.Services;
using StockManager.Domain.Entities;
using StockManager.Infrastructure.Data;

namespace StockManager.Infrastructure.Notificaciones;

/// <summary>
/// Genera el PDF de una Factura bajo demanda con QuestPDF, a partir de los datos ya
/// guardados de la Venta o el Pedido asociado. No se persiste ningún archivo: cada
/// solicitud (típicamente, la descarga que hace Twilio para adjuntarlo al WhatsApp) vuelve
/// a construir el documento desde la base de datos.
/// </summary>
public class QuestPdfFacturaService : IFacturaPdfService
{
    private readonly AppDbContext _db;

    private record LineaFactura(string NombreProducto, int Cantidad, decimal PrecioUnitario);

    public QuestPdfFacturaService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<byte[]?> GenerarPdfAsync(int facturaId)
    {
        var factura = await _db.Facturas.AsNoTracking().FirstOrDefaultAsync(f => f.Id == facturaId);
        if (factura is null || factura.Numero is null)
            return null;

        string nombreComprador;
        string? telefonoComprador;
        string? direccionEntrega = null;
        List<LineaFactura> lineas;

        if (factura.VentaId.HasValue)
        {
            var venta = await _db.Ventas.AsNoTracking().FirstOrDefaultAsync(v => v.Id == factura.VentaId.Value);
            if (venta is null)
                return null;

            Cliente? cliente = null;
            if (venta.ClienteId.HasValue)
                cliente = await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == venta.ClienteId.Value);

            nombreComprador = cliente?.Nombre ?? venta.NombreComprador ?? "Cliente";
            telefonoComprador = cliente?.Telefono ?? venta.TelefonoComprador;
            lineas = await ObtenerLineasVentaAsync(venta.Id);
        }
        else if (factura.PedidoId.HasValue)
        {
            var pedido = await _db.Pedidos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == factura.PedidoId.Value);
            if (pedido is null)
                return null;

            var cliente = await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == pedido.ClienteId);
            nombreComprador = cliente?.Nombre ?? "Cliente";
            telefonoComprador = cliente?.Telefono;
            direccionEntrega = pedido.Direccion;
            lineas = await ObtenerLineasPedidoAsync(pedido.Id);
        }
        else
        {
            return null; // No debería pasar: Factura exige exactamente una de las dos referencias.
        }

        return ConstruirPdf(factura, nombreComprador, telefonoComprador, direccionEntrega, lineas);
    }

    private async Task<List<LineaFactura>> ObtenerLineasVentaAsync(int ventaId)
    {
        var detalles = await _db.DetallesVenta.AsNoTracking().Where(d => d.VentaId == ventaId).ToListAsync();
        var nombresPorProducto = await ObtenerNombresProductosAsync(detalles.Select(d => d.ProductoId));
        return detalles
            .Select(d => new LineaFactura(nombresPorProducto.GetValueOrDefault(d.ProductoId, $"Producto #{d.ProductoId}"), d.Cantidad, d.PrecioUnitario))
            .ToList();
    }

    private async Task<List<LineaFactura>> ObtenerLineasPedidoAsync(int pedidoId)
    {
        var detalles = await _db.DetallesPedido.AsNoTracking().Where(d => d.PedidoId == pedidoId).ToListAsync();
        var nombresPorProducto = await ObtenerNombresProductosAsync(detalles.Select(d => d.ProductoId));
        return detalles
            .Select(d => new LineaFactura(nombresPorProducto.GetValueOrDefault(d.ProductoId, $"Producto #{d.ProductoId}"), d.Cantidad, d.PrecioUnitario))
            .ToList();
    }

    private async Task<Dictionary<int, string>> ObtenerNombresProductosAsync(IEnumerable<int> productoIds)
    {
        var ids = productoIds.Distinct().ToList();
        return await _db.Productos.AsNoTracking()
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.Nombre);
    }

    private static byte[] ConstruirPdf(
        Factura factura,
        string nombreComprador,
        string? telefonoComprador,
        string? direccionEntrega,
        List<LineaFactura> lineas)
    {
        var documento = Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(2, Unit.Centimetre);
                pagina.DefaultTextStyle(x => x.FontSize(11));

                pagina.Header().Column(col =>
                {
                    col.Item().Text("Ferretería Gold").FontSize(20).Bold();
                    col.Item().Text($"Factura {factura.Numero}").FontSize(14);
                    col.Item().Text($"Fecha: {factura.Fecha:dd/MM/yyyy HH:mm}");
                });

                pagina.Content().Column(col =>
                {
                    col.Spacing(10);

                    col.Item().PaddingTop(10).Text($"Cliente: {nombreComprador}");
                    if (!string.IsNullOrWhiteSpace(telefonoComprador))
                        col.Item().Text($"Teléfono: {telefonoComprador}");
                    if (!string.IsNullOrWhiteSpace(direccionEntrega))
                        col.Item().Text($"Dirección de entrega: {direccionEntrega}");

                    col.Item().PaddingTop(10).Table(tabla =>
                    {
                        tabla.ColumnsDefinition(columnas =>
                        {
                            columnas.RelativeColumn(4);
                            columnas.RelativeColumn(1);
                            columnas.RelativeColumn(2);
                            columnas.RelativeColumn(2);
                        });

                        tabla.Header(encabezado =>
                        {
                            encabezado.Cell().Text("Producto").Bold();
                            encabezado.Cell().Text("Cant.").Bold();
                            encabezado.Cell().Text("Precio unit.").Bold();
                            encabezado.Cell().Text("Subtotal").Bold();
                        });

                        foreach (var linea in lineas)
                        {
                            tabla.Cell().Text(linea.NombreProducto);
                            tabla.Cell().Text(linea.Cantidad.ToString());
                            tabla.Cell().Text(linea.PrecioUnitario.ToString("N0"));
                            tabla.Cell().Text((linea.Cantidad * linea.PrecioUnitario).ToString("N0"));
                        }
                    });

                    col.Item().PaddingTop(10).AlignRight().Text($"Total: ${factura.Total:N0}").FontSize(14).Bold();
                });

                pagina.Footer().AlignCenter().Text("Gracias por su compra.");
            });
        });

        return documento.GeneratePdf();
    }
}
