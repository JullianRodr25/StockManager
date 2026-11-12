using System.Reflection;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Common;

namespace StockManager.Infrastructure.Notificaciones;

/// <summary>
/// Genera el PDF de una Factura bajo demanda con QuestPDF. No decide qué datos mostrar: los
/// recibe ya armados de <see cref="IFacturaDocumentoService"/> (el mismo modelo que usan el
/// tiquete térmico y la vista digital) y solo se ocupa de la maquetación, que sigue el formato
/// de la factura de Ferretería Gold: logo, datos del emisor y del cliente, tabla de ítems,
/// totales y firmas. No se persiste ningún archivo: cada solicitud reconstruye el documento.
///
/// Importante: es la factura de venta del sistema, NO una factura electrónica validada por la
/// DIAN. Por eso no lleva CUFE, código QR ni "Representación gráfica": esos datos los emite el
/// proveedor tecnológico (Datico) y esta aplicación no los tiene.
/// </summary>
public class QuestPdfFacturaService : IFacturaPdfService
{
    private const string RecursoLogo = "StockManager.Recursos.logo-factura.jpg";
    private const string ColorTexto = "#222222";
    private const string ColorSecundario = "#555555";
    private const string ColorLinea = "#222222";

    // Se lee una sola vez: es un recurso embebido que no cambia mientras corre la aplicación.
    private static readonly Lazy<byte[]?> Logo = new(CargarLogo);

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

    public async Task<byte[]?> GenerarPdfDeVentaAsync(int ventaId)
    {
        var documento = await _documentoService.ObtenerPorVentaIdAsync(ventaId);
        return documento is null ? null : ConstruirPdf(documento);
    }

    private static byte[]? CargarLogo()
    {
        using var flujo = Assembly.GetExecutingAssembly().GetManifestResourceStream(RecursoLogo);
        if (flujo is null) return null;
        using var memoria = new MemoryStream();
        flujo.CopyTo(memoria);
        return memoria.ToArray();
    }

    // Las fechas se guardan en UTC: se muestran en hora de Colombia (ver HoraColombia).
    private static DateTime AHoraColombia(DateTime utc) => HoraColombia.ALocal(utc);

    private static string Moneda(decimal valor) => "$" + valor.ToString("N2", System.Globalization.CultureInfo.InvariantCulture);

    private static byte[] ConstruirPdf(FacturaDocumentoResponse doc)
    {
        var pdf = Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                pagina.Size(PageSizes.Letter);
                pagina.MarginHorizontal(1.2f, Unit.Centimetre);
                pagina.MarginVertical(1.2f, Unit.Centimetre);
                pagina.DefaultTextStyle(x => x.FontSize(9).FontColor(ColorTexto));

                pagina.Content().Column(col =>
                {
                    ComponerEncabezado(col, doc);
                    col.Item().PaddingTop(18).Element(c => ComponerPartes(c, doc));
                    col.Item().PaddingTop(22).Element(c => ComponerItems(c, doc));
                    col.Item().PaddingTop(8).Element(c => ComponerTotales(c, doc));
                    col.Item().PaddingTop(46).Element(ComponerFirmas);
                    col.Item().PaddingTop(20).Element(c => ComponerNotas(c, doc));
                });

                pagina.Footer().Row(fila =>
                {
                    fila.RelativeItem().AlignBottom().Text("Gracias por su compra.").FontSize(8).FontColor(ColorSecundario);
                    fila.ConstantItem(90).AlignRight().Text(t =>
                    {
                        t.DefaultTextStyle(x => x.FontSize(8).FontColor(ColorSecundario));
                        t.Span("PÁGINA ");
                        t.CurrentPageNumber();
                        t.Span(" / ");
                        t.TotalPages();
                    });
                });
            });
        });

        return pdf.GeneratePdf();
    }

    private static void ComponerEncabezado(ColumnDescriptor col, FacturaDocumentoResponse doc)
    {
        var e = doc.Emisor;

        col.Item().Row(fila =>
        {
            // Izquierda: logo y los textos tributarios del negocio (los de la Configuración).
            fila.RelativeItem(11).Column(izq =>
            {
                if (Logo.Value is { } logo)
                    izq.Item().Width(95).Image(logo).FitWidth();

                izq.Item().PaddingTop(3).Column(textos =>
                {
                    textos.Spacing(1);
                    if (e.ActividadEconomica is not null)
                        textos.Item().Text($"Actividad Económica Principal {e.ActividadEconomica}").FontSize(7).FontColor(ColorSecundario);
                    if (e.ResponsabilidadIva is not null)
                        textos.Item().Text(e.ResponsabilidadIva).FontSize(7).FontColor(ColorSecundario);
                    if (e.ResolucionDianTexto is not null)
                        textos.Item().Text(e.ResolucionDianTexto).FontSize(7).FontColor(ColorSecundario);
                });
            });

            // Derecha: título, número y datos de la operación.
            fila.RelativeItem(10).Column(der =>
            {
                der.Item().Row(titulo =>
                {
                    titulo.RelativeItem().Text("Factura de Venta").FontSize(15);
                    titulo.AutoItem().Text(doc.Numero).FontSize(13);
                });

                der.Item().PaddingTop(10).Column(datos =>
                {
                    datos.Spacing(1);
                    Dato(datos, "Fecha de Generación", AHoraColombia(doc.Fecha).ToString("dd/MM/yyyy HH:mm"));
                    if (doc.Pagos.Count > 0)
                    {
                        Dato(datos, "Forma de Pago", "Contado");
                        Dato(datos, "Medio de Pago", doc.Pagos.Count > 1 ? "Mixto" : doc.Pagos[0].MetodoPago);
                    }
                    if (doc.Vendedor is not null)
                        Dato(datos, "Vendedor", doc.Vendedor);
                    Dato(datos, "Moneda", "COP");
                });
            });
        });
    }

    private static void Dato(ColumnDescriptor col, string etiqueta, string valor)
    {
        col.Item().Row(f =>
        {
            f.RelativeItem().Text(etiqueta).FontSize(8);
            f.RelativeItem().AlignRight().Text(valor).FontSize(8);
        });
    }

    private static void ComponerPartes(IContainer contenedor, FacturaDocumentoResponse doc)
    {
        var e = doc.Emisor;
        var c = doc.Comprador;
        var direccionEmisor = string.Join(", ", new[] { e.Direccion, e.Barrio, e.Ciudad }.Where(p => !string.IsNullOrWhiteSpace(p)));

        contenedor.Row(fila =>
        {
            fila.RelativeItem().PaddingRight(10).Column(emisor =>
            {
                Titulo(emisor, "DATOS DEL EMISOR");
                Campo(emisor, "Razón Social", e.Nombre);
                Campo(emisor, "NIT", e.Nit);
                Campo(emisor, "Email", e.Email);
                Campo(emisor, "Teléfono", e.Telefono);
                Campo(emisor, "Dirección Fiscal", direccionEmisor);
            });

            fila.ConstantItem(0.5f).Background(ColorLinea);

            fila.RelativeItem().PaddingLeft(10).Column(cliente =>
            {
                Titulo(cliente, "DATOS DEL CLIENTE");
                Campo(cliente, "Razón Social", c.Nombre);
                Campo(cliente, c.TipoDocumento ?? "NIT / Documento", c.Documento);
                Campo(cliente, "Teléfono", c.Telefono);
                Campo(cliente, "Dirección", c.Direccion);
                Campo(cliente, "Entrega", c.DireccionEntrega);
            });
        });
    }

    private static void Titulo(ColumnDescriptor col, string texto)
    {
        col.Item().BorderBottom(0.5f).BorderColor(ColorLinea).PaddingBottom(4).AlignCenter()
            .Text(texto).FontSize(12);
        col.Item().Height(6);
    }

    // Campos sin dato se omiten (el comprador "CLIENTE FINAL" no trae documento ni dirección).
    private static void Campo(ColumnDescriptor col, string etiqueta, string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor)) return;
        col.Item().PaddingBottom(5).Row(f =>
        {
            f.ConstantItem(85).Text(etiqueta).FontSize(8.5f);
            f.RelativeItem().Text(valor).FontSize(8.5f);
        });
    }

    private static void ComponerItems(IContainer contenedor, FacturaDocumentoResponse doc)
    {
        contenedor.Table(tabla =>
        {
            tabla.ColumnsDefinition(c =>
            {
                c.ConstantColumn(24);   // No
                c.RelativeColumn(7);    // Descripción
                c.ConstantColumn(38);   // Cant
                c.ConstantColumn(34);   // U/M
                c.RelativeColumn(2);    // Precio
                c.ConstantColumn(38);   // IMP
                c.RelativeColumn(2);    // Total
            });

            static IContainer Cab(IContainer c) =>
                c.BorderTop(0.5f).BorderBottom(0.5f).BorderColor(ColorLinea).PaddingVertical(4);

            tabla.Header(h =>
            {
                h.Cell().Element(Cab).Text("No").FontSize(8).Bold();
                h.Cell().Element(Cab).Text("DESCRIPCIÓN").FontSize(10).Bold();
                h.Cell().Element(Cab).AlignRight().Text("CANT").FontSize(10).Bold();
                h.Cell().Element(Cab).AlignCenter().Text("U/M").FontSize(10).Bold();
                h.Cell().Element(Cab).AlignRight().Text("PRECIO").FontSize(10).Bold();
                h.Cell().Element(Cab).AlignRight().Text("IMP").FontSize(10).Bold();
                h.Cell().Element(Cab).AlignRight().Text("TOTAL ITEM").FontSize(10).Bold();
            });

            var numero = 0;
            foreach (var l in doc.Lineas)
            {
                numero++;
                static IContainer Fila(IContainer c) => c.PaddingVertical(3);
                tabla.Cell().Element(Fila).Text(numero.ToString()).FontSize(8);
                tabla.Cell().Element(Fila).Text(l.Producto).FontSize(8);
                tabla.Cell().Element(Fila).AlignRight().Text(l.Cantidad.ToString()).FontSize(8);
                tabla.Cell().Element(Fila).AlignCenter().Text("EA").FontSize(7);
                tabla.Cell().Element(Fila).AlignRight().Text(Moneda(l.PrecioUnitario)).FontSize(8);
                tabla.Cell().Element(Fila).AlignRight().Text(l.TarifaIva > 0 ? $"{l.TarifaIva:0.##}%" : "").FontSize(8);
                tabla.Cell().Element(Fila).AlignRight().Text(Moneda(l.Total)).FontSize(8);
            }
        });
    }

    private static void ComponerTotales(IContainer contenedor, FacturaDocumentoResponse doc)
    {
        contenedor.Row(fila =>
        {
            fila.RelativeItem(11).AlignBottom().Text("Unidades de medida: EA = cada").FontSize(7).FontColor(ColorSecundario);

            fila.RelativeItem(10).Column(t =>
            {
                t.Spacing(3);
                t.Item().Row(f =>
                {
                    f.RelativeItem().Text("Subtotal").FontSize(10);
                    f.RelativeItem().AlignRight().Text(Moneda(doc.SubtotalBase)).FontSize(10);
                });

                foreach (var r in doc.ResumenIva.Where(r => r.Tarifa > 0))
                {
                    t.Item().Row(f =>
                    {
                        f.RelativeItem().Text($"IVA {r.Tarifa:0.##}% (base {Moneda(r.Base)})").FontSize(8.5f);
                        f.RelativeItem().AlignRight().Text(Moneda(r.Iva)).FontSize(8.5f);
                    });
                }

                t.Item().BorderTop(0.5f).BorderColor(ColorLinea).PaddingTop(5).Row(f =>
                {
                    f.RelativeItem().Text("Total a Pagar").FontSize(13);
                    f.RelativeItem().AlignRight().Text(Moneda(doc.Total)).FontSize(13);
                });

                if (doc.MontoRecibido.HasValue)
                {
                    t.Item().Row(f =>
                    {
                        f.RelativeItem().Text("Recibido").FontSize(8.5f);
                        f.RelativeItem().AlignRight().Text(Moneda(doc.MontoRecibido.Value)).FontSize(8.5f);
                    });
                }
                if (doc.Cambio.HasValue)
                {
                    t.Item().Row(f =>
                    {
                        f.RelativeItem().Text("Cambio").FontSize(8.5f);
                        f.RelativeItem().AlignRight().Text(Moneda(doc.Cambio.Value)).FontSize(8.5f);
                    });
                }
            });
        });
    }

    private static void ComponerFirmas(IContainer contenedor)
    {
        contenedor.Row(fila =>
        {
            fila.RelativeItem().PaddingHorizontal(10).Column(c =>
            {
                c.Item().BorderBottom(0.5f).BorderColor(ColorLinea).Height(1);
                c.Item().PaddingTop(5).AlignCenter().Text("FIRMA EMISOR").FontSize(8.5f).FontColor(ColorSecundario);
            });
            fila.RelativeItem().PaddingHorizontal(10).Column(c =>
            {
                c.Item().BorderBottom(0.5f).BorderColor(ColorLinea).Height(1);
                c.Item().PaddingTop(5).AlignCenter().Text("FIRMA CLIENTE").FontSize(8.5f).FontColor(ColorSecundario);
            });
        });
    }

    private static void ComponerNotas(IContainer contenedor, FacturaDocumentoResponse doc)
    {
        contenedor.Column(c =>
        {
            c.Spacing(3);
            if (doc.TextoLegal is not null)
                c.Item().Text(doc.TextoLegal).FontSize(7.5f).FontColor(ColorSecundario);
            if (doc.PoliticaCambios is not null)
                c.Item().Text(doc.PoliticaCambios).FontSize(7.5f).FontColor(ColorSecundario);
        });
    }
}
