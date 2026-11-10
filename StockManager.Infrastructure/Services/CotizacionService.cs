using Microsoft.EntityFrameworkCore;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Common;
using StockManager.Domain.Entities;
using StockManager.Domain.Exceptions;
using StockManager.Infrastructure.Data;

namespace StockManager.Infrastructure.Services;

/// <summary>
/// Casos de uso de cotizaciones (crear, editar, consultar, anular, PDF). Las reglas del propio
/// agregado (estados, plazo, líneas) viven en <see cref="Cotizacion"/>; aquí solo está lo que
/// depende de otros datos: que los productos existan y estén activos, quién puede fijar un
/// precio distinto al de lista y los nombres para mostrar. La conversión en venta vive en
/// VentaService porque comparte con el registro de venta la transacción, el stock y la caja.
/// </summary>
public class CotizacionService : ICotizacionService
{
    private readonly AppDbContext _db;
    private readonly ICotizacionPdfService _pdf;

    public CotizacionService(AppDbContext db, ICotizacionPdfService pdf)
    {
        _db = db;
        _pdf = pdf;
    }

    private static DateOnly Hoy() => DateOnly.FromDateTime(HoraColombia.Hoy());

    public static string FormatearNumero(int id) => $"COT-{id:D5}";

    public async Task<CotizacionResponse> CrearAsync(GuardarCotizacionRequest request, int empleadoId, bool esAdmin)
    {
        var contenido = await PrepararContenidoAsync(request, preciosVigentes: null, esAdmin);

        var cotizacion = Cotizacion.Crear(
            empleadoId,
            request.ClienteId,
            contenido.NombreCliente,
            contenido.TelefonoCliente,
            contenido.EmailCliente,
            request.FechaVencimiento,
            request.Notas,
            contenido.Lineas,
            Hoy());

        _db.Cotizaciones.Add(cotizacion);
        await _db.SaveChangesAsync();

        return await ObtenerAsync(cotizacion.Id);
    }

    public async Task<CotizacionResponse> ActualizarAsync(int id, GuardarCotizacionRequest request, bool esAdmin)
    {
        var cotizacion = await _db.Cotizaciones.Include(c => c.Detalles).FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new CotizacionNoEncontradaException(id);

        // Los precios ya cotizados pueden conservarse al editar sin ser Admin.
        var preciosVigentes = cotizacion.Detalles.ToDictionary(d => d.ProductoId, d => d.PrecioUnitario);
        var contenido = await PrepararContenidoAsync(request, preciosVigentes, esAdmin);

        cotizacion.Editar(
            request.ClienteId,
            contenido.NombreCliente,
            contenido.TelefonoCliente,
            contenido.EmailCliente,
            request.FechaVencimiento,
            request.Notas,
            contenido.Lineas,
            Hoy());

        await _db.SaveChangesAsync();
        return await ObtenerAsync(id);
    }

    public async Task<CotizacionResponse> AnularAsync(int id)
    {
        var cotizacion = await _db.Cotizaciones.FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new CotizacionNoEncontradaException(id);

        cotizacion.Anular();
        await _db.SaveChangesAsync();
        return await ObtenerAsync(id);
    }

    public async Task<CotizacionResponse> ObtenerAsync(int id)
    {
        var cotizacion = await _db.Cotizaciones.AsNoTracking().Include(c => c.Detalles)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new CotizacionNoEncontradaException(id);

        var productoIds = cotizacion.Detalles.Select(d => d.ProductoId).ToList();
        var productos = await _db.Productos.AsNoTracking()
            .Where(p => productoIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        var empleado = await _db.Empleados.AsNoTracking()
            .Where(e => e.Id == cotizacion.EmpleadoId)
            .Select(e => e.Nombre)
            .FirstOrDefaultAsync() ?? string.Empty;

        string? numeroFactura = null;
        if (cotizacion.VentaId.HasValue)
            numeroFactura = await _db.Facturas.AsNoTracking()
                .Where(f => f.VentaId == cotizacion.VentaId.Value)
                .Select(f => f.Numero)
                .FirstOrDefaultAsync();

        var lineas = cotizacion.Detalles
            .Select(d =>
            {
                var producto = productos.GetValueOrDefault(d.ProductoId);
                return new LineaCotizacionResponse(
                    d.ProductoId,
                    producto?.Nombre ?? $"Producto {d.ProductoId}",
                    d.Cantidad,
                    d.PrecioUnitario,
                    d.Subtotal,
                    producto?.Precio ?? d.PrecioUnitario,
                    producto?.StockActual ?? 0);
            })
            .ToList();

        return new CotizacionResponse(
            cotizacion.Id,
            FormatearNumero(cotizacion.Id),
            cotizacion.EmpleadoId,
            empleado,
            cotizacion.ClienteId,
            cotizacion.NombreCliente,
            cotizacion.TelefonoCliente,
            cotizacion.EmailCliente,
            cotizacion.Fecha,
            cotizacion.FechaVencimiento,
            cotizacion.EstadoEfectivo(Hoy()),
            cotizacion.Notas,
            cotizacion.Total,
            cotizacion.VentaId,
            numeroFactura,
            cotizacion.FechaConversion,
            lineas);
    }

    public async Task<(List<CotizacionResumenResponse> Items, int Total)> ListarAsync(
        int pagina, int tamanoPagina, string? estado, string? busqueda, DateOnly? desde, DateOnly? hasta)
    {
        var hoy = Hoy();
        var consulta = _db.Cotizaciones.AsNoTracking().AsQueryable();

        // "Vencida" no se guarda: es una cotización Vigente cuyo plazo ya pasó.
        consulta = estado switch
        {
            Cotizacion.EstadoVigente => consulta.Where(c => c.Estado == Cotizacion.EstadoVigente && c.FechaVencimiento >= hoy),
            Cotizacion.EstadoVencida => consulta.Where(c => c.Estado == Cotizacion.EstadoVigente && c.FechaVencimiento < hoy),
            Cotizacion.EstadoConvertida or Cotizacion.EstadoAnulada => consulta.Where(c => c.Estado == estado),
            _ => consulta
        };

        if (desde.HasValue)
        {
            var inicio = HoraColombia.AUtc(desde.Value.ToDateTime(TimeOnly.MinValue));
            consulta = consulta.Where(c => c.Fecha >= inicio);
        }

        if (hasta.HasValue)
        {
            var fin = HoraColombia.AUtc(hasta.Value.AddDays(1).ToDateTime(TimeOnly.MinValue));
            consulta = consulta.Where(c => c.Fecha < fin);
        }

        if (!string.IsNullOrWhiteSpace(busqueda))
        {
            var texto = busqueda.Trim();
            var digitos = new string(texto.Where(char.IsDigit).ToArray());
            var id = digitos.Length is > 0 and <= 9 ? int.Parse(digitos) : (int?)null;
            consulta = consulta.Where(c =>
                (c.NombreCliente != null && c.NombreCliente.Contains(texto)) || (id != null && c.Id == id));
        }

        var total = await consulta.CountAsync();

        var filas = await consulta
            .OrderByDescending(c => c.Fecha)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Select(c => new
            {
                c.Id, c.NombreCliente, c.EmpleadoId, c.Fecha, c.FechaVencimiento, c.Estado, c.Total,
                c.VentaId, Lineas = c.Detalles.Count
            })
            .ToListAsync();

        var empleadoIds = filas.Select(f => f.EmpleadoId).Distinct().ToList();
        var empleados = await _db.Empleados.AsNoTracking()
            .Where(e => empleadoIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.Nombre);

        var items = filas.Select(f => new CotizacionResumenResponse(
            f.Id,
            FormatearNumero(f.Id),
            f.NombreCliente,
            empleados.GetValueOrDefault(f.EmpleadoId, string.Empty),
            f.Fecha,
            f.FechaVencimiento,
            f.Estado == Cotizacion.EstadoVigente && f.FechaVencimiento < hoy ? Cotizacion.EstadoVencida : f.Estado,
            f.Total,
            f.Lineas,
            f.VentaId)).ToList();

        return (items, total);
    }

    public async Task<byte[]> GenerarPdfAsync(int id)
    {
        var cotizacion = await ObtenerAsync(id);
        return await _pdf.GenerarAsync(cotizacion);
    }

    private sealed record ContenidoValidado(
        string? NombreCliente,
        string? TelefonoCliente,
        string? EmailCliente,
        List<(int ProductoId, int Cantidad, decimal PrecioUnitario)> Lineas);

    /// <summary>
    /// Valida productos y precios y resuelve los datos del cliente. Un cliente registrado aporta
    /// su nombre/teléfono/correo actuales como foto en la cotización (si cambian después, la
    /// cotización ya entregada no se altera sola).
    /// </summary>
    private async Task<ContenidoValidado> PrepararContenidoAsync(
        GuardarCotizacionRequest request,
        IReadOnlyDictionary<int, decimal>? preciosVigentes,
        bool esAdmin)
    {
        if (request.Lineas is null || request.Lineas.Count == 0)
            throw new ArgumentException("La cotización debe incluir al menos un producto.");

        // Una misma referencia repetida se suma en una sola línea.
        var agrupadas = request.Lineas
            .GroupBy(l => l.ProductoId)
            .Select(g => new { ProductoId = g.Key, Cantidad = g.Sum(l => l.Cantidad), Precio = g.First().PrecioUnitario })
            .ToList();

        var ids = agrupadas.Select(l => l.ProductoId).ToList();
        var productos = await _db.Productos.AsNoTracking().Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id);

        var lineas = new List<(int, int, decimal)>();
        foreach (var linea in agrupadas)
        {
            if (!productos.TryGetValue(linea.ProductoId, out var producto))
                throw new ArgumentException($"El producto con ID {linea.ProductoId} no existe.");

            if (!producto.Activo)
                throw new ProductoInactivoException(producto.Id, producto.Nombre);

            if (linea.Cantidad <= 0)
                throw new ArgumentException($"La cantidad para el producto '{producto.Nombre}' debe ser mayor a 0.");

            var precio = linea.Precio ?? producto.Precio;
            if (precio <= 0)
                throw new ArgumentException($"El precio de '{producto.Nombre}' debe ser mayor a 0.");

            // Un precio distinto al de lista es un descuento: solo un Admin lo fija. Conservar
            // el precio ya cotizado de una línea existente tampoco requiere ser Admin.
            var esConservado = preciosVigentes is not null
                               && preciosVigentes.TryGetValue(producto.Id, out var anterior)
                               && anterior == precio;
            if (precio != producto.Precio && !esConservado && !esAdmin)
                throw new ArgumentException(
                    $"Solo un administrador puede cotizar '{producto.Nombre}' a un precio distinto al de lista.");

            lineas.Add((producto.Id, linea.Cantidad, precio));
        }

        string? nombre = request.NombreCliente, telefono = request.TelefonoCliente, email = request.EmailCliente;
        if (request.ClienteId.HasValue)
        {
            var cliente = await _db.Clientes.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.ClienteId.Value)
                ?? throw new ArgumentException($"El cliente con ID {request.ClienteId.Value} no existe.");

            nombre = cliente.Nombre;
            telefono = cliente.Telefono;
            email = cliente.Email;
        }

        return new ContenidoValidado(nombre, telefono, email, lineas);
    }
}
