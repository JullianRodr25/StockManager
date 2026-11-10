using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Common;
using StockManager.Infrastructure.Data;

namespace StockManager.Infrastructure.Services;

/// <summary>
/// PDF de cotización con QuestPDF, bajo demanda y sin persistir archivos (igual que la factura).
/// Los datos del emisor salen de la configuración de la empresa.
/// </summary>
public class QuestPdfCotizacionService : ICotizacionPdfService
{
    private readonly AppDbContext _db;

    public QuestPdfCotizacionService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<byte[]> GenerarAsync(CotizacionResponse c)
    {
        var config = await _db.Configuraciones.AsNoTracking().FirstOrDefaultAsync();

        var direccion = string.Join(" - ",
            new[] { config?.DireccionEmpresa, config?.BarrioEmpresa, config?.CiudadEmpresa }
                .Where(p => !string.IsNullOrWhiteSpace(p)));
        var contacto = string.Join(" · ", new[]
            {
                string.IsNullOrWhiteSpace(config?.TelefonoEmpresa) ? null : $"Tel. {config!.TelefonoEmpresa}",
                config?.EmailEmpresa
            }.Where(p => !string.IsNullOrWhiteSpace(p)));

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

                    col.Item().AlignCenter().Text(config?.NombreEmpresa ?? "Ferretería Gold").FontSize(18).Bold();
                    if (!string.IsNullOrWhiteSpace(config?.NitEmpresa))
                        col.Item().AlignCenter().Text($"NIT {config!.NitEmpresa}");
                    if (direccion.Length > 0)
                        col.Item().AlignCenter().Text(direccion);
                    if (contacto.Length > 0)
                        col.Item().AlignCenter().Text(contacto);

                    col.Item().PaddingTop(10).Text(t =>
                    {
                        t.Span("COTIZACIÓN No. ").Bold().FontSize(13);
                        t.Span(c.Numero).FontSize(13);
                    });
                    col.Item().Text($"Fecha: {HoraColombia.ALocal(c.Fecha):dd/MM/yyyy}");
                    col.Item().Text(t =>
                    {
                        t.Span("Válida hasta: ").Bold();
                        t.Span($"{c.FechaVencimiento:dd/MM/yyyy}");
                    });
                    col.Item().Text($"Atendido por: {c.EmpleadoNombre}");

                    col.Item().PaddingTop(4).LineHorizontal(0.5f);
                    col.Item().Text($"Cliente: {c.NombreCliente ?? "—"}");
                    if (!string.IsNullOrWhiteSpace(c.TelefonoCliente))
                        col.Item().Text($"Teléfono: {c.TelefonoCliente}");
                    if (!string.IsNullOrWhiteSpace(c.EmailCliente))
                        col.Item().Text($"Correo: {c.EmailCliente}");

                    col.Item().PaddingTop(6).Table(tabla =>
                    {
                        tabla.ColumnsDefinition(columnas =>
                        {
                            columnas.RelativeColumn(6);
                            columnas.RelativeColumn(1);
                            columnas.RelativeColumn(2);
                            columnas.RelativeColumn(2);
                        });

                        tabla.Header(encabezado =>
                        {
                            encabezado.Cell().BorderBottom(0.5f).Text("Producto").Bold();
                            encabezado.Cell().BorderBottom(0.5f).AlignRight().Text("Cant.").Bold();
                            encabezado.Cell().BorderBottom(0.5f).AlignRight().Text("Precio unit.").Bold();
                            encabezado.Cell().BorderBottom(0.5f).AlignRight().Text("Total").Bold();
                        });

                        foreach (var l in c.Lineas)
                        {
                            tabla.Cell().PaddingVertical(2).Text(l.ProductoNombre);
                            tabla.Cell().PaddingVertical(2).AlignRight().Text(l.Cantidad.ToString());
                            tabla.Cell().PaddingVertical(2).AlignRight().Text($"$ {l.PrecioUnitario:N0}");
                            tabla.Cell().PaddingVertical(2).AlignRight().Text($"$ {l.Subtotal:N0}");
                        }
                    });

                    col.Item().PaddingTop(4).LineHorizontal(0.5f);
                    col.Item().AlignRight().Text($"TOTAL: $ {c.Total:N0}").FontSize(13).Bold();

                    if (!string.IsNullOrWhiteSpace(c.Notas))
                    {
                        col.Item().PaddingTop(8).Text("Notas").Bold();
                        col.Item().Text(c.Notas);
                    }

                    col.Item().PaddingTop(14).Text(
                            $"Precios en pesos colombianos, IVA incluido cuando aplica. Oferta válida hasta el {c.FechaVencimiento:dd/MM/yyyy} y sujeta a disponibilidad de inventario.")
                        .FontSize(8).Italic();
                });
            });
        });

        return pdf.GeneratePdf();
    }
}
