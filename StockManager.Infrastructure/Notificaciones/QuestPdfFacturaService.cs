using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StockManager.Application.DTOs;
using StockManager.Application.Services;

namespace StockManager.Infrastructure.Notificaciones;

/// <summary>
/// Genera el PDF de una Factura bajo demanda con QuestPDF. No decide qué datos mostrar: los
/// recibe ya armados de <see cref="IFacturaDocumentoService"/> (el mismo modelo que usan el
/// tiquete térmico y la vista digital) y solo se ocupa de la maquetación. No se persiste
/// ningún archivo: cada solicitud vuelve a construir el documento.
/// </summary>
public class QuestPdfFacturaService : IFacturaPdfService
{
    private readonly IFacturaDocumentoService _documentoService;

    public QuestPdfFacturaService(IFacturaDocumentoService documentoService)
    {
        _documentoService = documentoService;
    }

    public async Task<byte[]?> GenerarPdfAsync(int facturaId)
    {
        var documento = await _documentoService.ObtenerPorFacturaIdAsync(facturaId);
        return documento is null ? null : ConstruirPdf(documento);
    }

    private static byte[] ConstruirPdf(FacturaDocumentoResponse doc)
    {
        var pdf = Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                pagina.Size(PageSizes.A4);
                pagina.Margin(2, Unit.Centimetre);
                pagina.DefaultTextStyle(x => x.FontSize(10));

                pagina.Content().Column(col =>
                {
                    col.Spacing(6);
                    ComponerEncabezado(col, doc);
                    ComponerComprador(col, doc);
                    ComponerLineas(col, doc);
                    ComponerTotales(col, doc);
                    ComponerPie(col, doc);
                });
            });
        });

        return pdf.GeneratePdf();
    }

    // Las fechas se guardan en UTC; el servidor (Azure) también corre en UTC, así que
    // ToLocalTime() no serviría: se convierte explícitamente a la hora del negocio.
    private static DateTime AHoraColombia(DateTime utc)
    {
        try
        {
            return TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(utc, DateTimeKind.Utc),
                TimeZoneInfo.FindSystemTimeZoneById("America/Bogota"));
        }
        catch (TimeZoneNotFoundException)
        {
            return utc.AddHours(-5); // Colombia no tiene horario de verano: UTC-5 fijo.
        }
    }

    private static void ComponerEncabezado(ColumnDescriptor col, FacturaDocumentoResponse doc)
    {
        var e = doc.Emisor;

        col.Item().AlignCenter().Text(e.Nombre ?? "Ferretería Gold").FontSize(18).Bold();
        if (e.Nit is not null)
            col.Item().AlignCenter().Text($"NIT {e.Nit}");
        if (e.ResponsabilidadIva is not null)
            col.Item().AlignCenter().Text(e.ResponsabilidadIva);
        if (e.ActividadEconomica is not null)
            col.Item().AlignCenter().Text($"Actividad económica: {e.ActividadEconomica}");

        var direccion = string.Join(" - ", new[] { e.Direccion, e.Barrio, e.Ciudad }.Where(p => p is not null));
        if (direccion.Length > 0)
            col.Item().AlignCenter().Text(direccion);

        var contacto = string.Join(" · ", new[] { e.Telefono is null ? null : $"Tel. {e.Telefono}", e.Email }.Where(p => p is not null));
        if (contacto.Length > 0)
            col.Item().AlignCenter().Text(contacto);

        if (e.ResolucionDianTexto is not null)
            col.Item().AlignCenter().Text(e.ResolucionDianTexto).FontSize(9);

        col.Item().PaddingTop(8).Text(t =>
        {
            t.Span("Factura de venta No. ").Bold();
            t.Span(doc.Numero);
        });
        col.Item().Text($"Fecha: {AHoraColombia(doc.Fecha):dd/MM/yyyy HH:mm}");
        if (doc.Vendedor is not null)
            col.Item().Text($"Vendedor: {doc.Vendedor}");
    }

    private static void ComponerComprador(ColumnDescriptor col, FacturaDocumentoResponse doc)
    {
        var c = doc.Comprador;

        col.Item().PaddingTop(4).LineHorizontal(0.5f);
        col.Item().Text($"Cliente: {c.Nombre}");
        if (c.Documento is not null)
            col.Item().Text($"{c.TipoDocumento ?? "Documento"}: {c.Documento}");
        if (c.Direccion is not null)
            col.Item().Text($"Dirección: {c.Direccion}");
        if (c.Telefono is not null)
            col.Item().Text($"Teléfono: {c.Telefono}");
        if (c.DireccionEntrega is not null)
            col.Item().Text($"Dirección de entrega: {c.DireccionEntrega}");
    }

    private static void ComponerLineas(ColumnDescriptor col, FacturaDocumentoResponse doc)
    {
        col.Item().PaddingTop(4).Table(tabla =>
        {
            tabla.ColumnsDefinition(columnas =>
            {
                columnas.RelativeColumn(5);
                columnas.RelativeColumn(1);
                columnas.RelativeColumn(2);
                columnas.RelativeColumn(1);
                columnas.RelativeColumn(2);
            });

            tabla.Header(encabezado =>
            {
                encabezado.Cell().BorderBottom(0.5f).Text("Producto").Bold();
                encabezado.Cell().BorderBottom(0.5f).AlignRight().Text("Cant.").Bold();
                encabezado.Cell().BorderBottom(0.5f).AlignRight().Text("Precio unit.").Bold();
                encabezado.Cell().BorderBottom(0.5f).AlignRight().Text("IVA").Bold();
                encabezado.Cell().BorderBottom(0.5f).AlignRight().Text("Total").Bold();
            });

            foreach (var linea in doc.Lineas)
            {
                tabla.Cell().Text(linea.Producto);
                tabla.Cell().AlignRight().Text(linea.Cantidad.ToString());
                tabla.Cell().AlignRight().Text(linea.PrecioUnitario.ToString("N0"));
                tabla.Cell().AlignRight().Text($"{linea.TarifaIva:0.##}%");
                tabla.Cell().AlignRight().Text(linea.Total.ToString("N0"));
            }
        });
    }

    private static void ComponerTotales(ColumnDescriptor col, FacturaDocumentoResponse doc)
    {
        col.Item().PaddingTop(6).AlignRight().Column(totales =>
        {
            totales.Spacing(2);

            foreach (var r in doc.ResumenIva.Where(r => r.Tarifa > 0))
            {
                totales.Item().AlignRight().Text($"Base IVA {r.Tarifa:0.##}%: ${r.Base:N0}");
                totales.Item().AlignRight().Text($"IVA {r.Tarifa:0.##}%: ${r.Iva:N0}");
            }

            totales.Item().AlignRight().Text($"TOTAL: ${doc.Total:N0}").FontSize(13).Bold();

            foreach (var pago in doc.Pagos)
                totales.Item().AlignRight().Text($"{pago.MetodoPago}: ${pago.Monto:N0}");

            if (doc.MontoRecibido.HasValue)
                totales.Item().AlignRight().Text($"Recibido: ${doc.MontoRecibido.Value:N0}");
            if (doc.Cambio.HasValue)
                totales.Item().AlignRight().Text($"Cambio: ${doc.Cambio.Value:N0}");
        });
    }

    private static void ComponerPie(ColumnDescriptor col, FacturaDocumentoResponse doc)
    {
        if (doc.TextoLegal is not null)
            col.Item().PaddingTop(10).Text(doc.TextoLegal).FontSize(8);
        if (doc.PoliticaCambios is not null)
            col.Item().Text(doc.PoliticaCambios).FontSize(8);

        col.Item().PaddingTop(6).AlignCenter().Text("Gracias por su compra.");
    }
}
