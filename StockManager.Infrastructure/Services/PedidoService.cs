using Microsoft.EntityFrameworkCore;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Entities;
using StockManager.Domain.Exceptions;
using StockManager.Infrastructure.Data;

namespace StockManager.Infrastructure.Services;

public class PedidoService : IPedidoService
{
    private readonly AppDbContext _dbContext;

    public PedidoService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Crea un pedido a domicilio. Por cada línea con stock suficiente se descuenta el
    /// stock de inmediato (reserva, igual que una venta de mostrador); si no alcanza, la
    /// línea queda "PorEncargo" y se genera un BackorderRequest sin tocar el stock.
    /// </summary>
    public async Task<PedidoResponse> CrearPedidoAsync(CrearPedidoRequest request, int clienteId)
    {
        if (request.Lineas == null || request.Lineas.Count == 0)
            throw new ArgumentException("Debe incluir al menos un producto.");

        if (string.IsNullOrWhiteSpace(request.Direccion))
            throw new ArgumentException("La dirección no puede estar vacía.");

        var cliente = await _dbContext.Clientes.FirstOrDefaultAsync(c => c.Id == clienteId);
        if (cliente == null)
            throw new ArgumentException($"El cliente con ID {clienteId} no existe.");

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var calculos = new List<CalculoLineaPedido>();

        // FASE DE CÁLCULO: solo lee, no modifica stock ni crea nada todavía.
        foreach (var linea in request.Lineas)
        {
            if (linea.Cantidad <= 0)
                throw new ArgumentException("La cantidad de cada línea debe ser mayor a 0.");

            var producto = await _dbContext.Productos.FirstOrDefaultAsync(p => p.Id == linea.ProductoId);
            if (producto == null)
                throw new ArgumentException($"El producto con ID {linea.ProductoId} no existe.");

            if (!producto.Activo)
                throw new ProductoInactivoException(producto.Id, producto.Nombre);

            var disponible = producto.StockActual >= linea.Cantidad;
            calculos.Add(new CalculoLineaPedido(producto, linea.Cantidad, producto.Precio, disponible));
        }

        var total = calculos.Sum(c => c.PrecioUnitario * c.Cantidad);

        var pedido = Pedido.Crear(clienteId, request.Direccion, total);
        _dbContext.Pedidos.Add(pedido);
        await _dbContext.SaveChangesAsync();

        // Se crean primero todas las líneas y se guardan para obtener sus Id reales
        // (un BackorderRequest necesita el Id real de su DetallePedido para enlazarse).
        var lineasCreadas = new List<(DetallePedido Detalle, CalculoLineaPedido Calculo)>();
        foreach (var calculo in calculos)
        {
            var estadoLinea = calculo.Disponible ? "Disponible" : "PorEncargo";
            var detalle = DetallePedido.Crear(pedido.Id, calculo.Producto.Id, calculo.Cantidad, calculo.PrecioUnitario, estadoLinea);
            _dbContext.DetallesPedido.Add(detalle);
            lineasCreadas.Add((detalle, calculo));
        }
        await _dbContext.SaveChangesAsync();

        // FASE DE APLICACIÓN: reserva stock de las líneas disponibles y registra el
        // backorder de las que no alcanzaron.
        foreach (var (detalle, calculo) in lineasCreadas)
        {
            if (calculo.Disponible)
            {
                calculo.Producto.Vender(calculo.Cantidad);
                var movimiento = MovimientoStock.Crear(calculo.Producto.Id, "SalidaPedido", calculo.Cantidad, "Pedido", pedido.Id);
                _dbContext.MovimientosStock.Add(movimiento);
            }
            else
            {
                var backorder = BackorderRequest.Crear(clienteId, calculo.Producto.Id, calculo.Cantidad, detalle.Id);
                _dbContext.BackorderRequests.Add(backorder);
            }
        }

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyException(
                "El stock de uno de los productos cambió mientras se procesaba el pedido. Intenta de nuevo.");
        }

        await transaction.CommitAsync();

        return (await ObtenerPedidoPorIdAsync(pedido.Id))!;
    }

    public async Task<(List<PedidoResumenResponse> Items, int Total)> ObtenerPedidosPaginadoAsync(
        int pagina, int tamanoPagina, string? estado, int? clienteId)
    {
        var query = _dbContext.Pedidos.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(estado))
            query = query.Where(p => p.Estado == estado);

        if (clienteId.HasValue)
            query = query.Where(p => p.ClienteId == clienteId.Value);

        var total = await query.CountAsync();

        var pedidosPagina = await query
            .OrderByDescending(p => p.Fecha)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Join(
                _dbContext.Clientes.AsNoTracking(),
                pedido => pedido.ClienteId,
                cliente => cliente.Id,
                (pedido, cliente) => new { Pedido = pedido, ClienteNombre = cliente.Nombre })
            .ToListAsync();

        var idsPagina = pedidosPagina.Select(x => x.Pedido.Id).ToList();

        var idsConPorEncargo = (await _dbContext.DetallesPedido
            .AsNoTracking()
            .Where(d => idsPagina.Contains(d.PedidoId) && d.EstadoLinea == "PorEncargo")
            .Select(d => d.PedidoId)
            .Distinct()
            .ToListAsync())
            .ToHashSet();

        var items = pedidosPagina
            .Select(x => new PedidoResumenResponse(
                x.Pedido.Id,
                x.ClienteNombre,
                x.Pedido.Fecha,
                x.Pedido.Estado,
                x.Pedido.Total,
                idsConPorEncargo.Contains(x.Pedido.Id)))
            .ToList();

        return (items, total);
    }

    public async Task<PedidoResponse?> ObtenerPedidoPorIdAsync(int id)
    {
        var pedido = await _dbContext.Pedidos.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (pedido == null)
            return null;

        var clienteNombre = await _dbContext.Clientes
            .AsNoTracking()
            .Where(c => c.Id == pedido.ClienteId)
            .Select(c => c.Nombre)
            .FirstOrDefaultAsync() ?? string.Empty;

        var detalles = await ObtenerDetallesPedidoAsync(id);

        return new PedidoResponse(
            pedido.Id,
            pedido.ClienteId,
            clienteNombre,
            pedido.Direccion,
            pedido.Fecha,
            pedido.Estado,
            pedido.Total,
            pedido.VentaId,
            detalles);
    }

    public Task<PedidoResponse> ConfirmarAsync(int pedidoId) =>
        EjecutarTransicionAsync(pedidoId, p => p.Confirmar());

    public Task<PedidoResponse> IniciarPreparacionAsync(int pedidoId) =>
        EjecutarTransicionAsync(pedidoId, p => p.IniciarPreparacion());

    public Task<PedidoResponse> EnviarACaminoAsync(int pedidoId) =>
        EjecutarTransicionAsync(pedidoId, p => p.EnviarACamino());

    /// <summary>
    /// Marca el pedido como entregado: genera una Venta y su Factura con las mismas
    /// líneas y el mismo total del pedido, para que quede contabilizado en Historial de
    /// Ventas igual que cualquier venta de mostrador. El stock de las líneas "Disponible"
    /// ya se descontó al crear el pedido, así que aquí no se vuelve a tocar.
    /// </summary>
    public async Task<PedidoResponse> MarcarEntregadoAsync(int pedidoId, string metodoPago, int empleadoId)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var pedido = await _dbContext.Pedidos.FirstOrDefaultAsync(p => p.Id == pedidoId);
        if (pedido == null)
            throw new PedidoNoEncontradoException(pedidoId);

        var detalles = await _dbContext.DetallesPedido.Where(d => d.PedidoId == pedidoId).ToListAsync();
        if (detalles.Count == 0)
            throw new ArgumentException($"El pedido {pedidoId} no tiene líneas.");

        if (detalles.Any(d => d.EstadoLinea == "PorEncargo"))
            throw new OperacionInvalidaPedidoException(
                "No se puede entregar el pedido: todavía tiene productos 'Por encargo' pendientes de llegar.");

        var venta = Venta.Crear(
            empleadoId,
            pedido.ClienteId,
            nombreComprador: null,
            telefonoComprador: null,
            emailComprador: null,
            metodoPago,
            pedido.Total,
            esCotizacion: false,
            estado: "Pagada");

        _dbContext.Ventas.Add(venta);
        await _dbContext.SaveChangesAsync();

        foreach (var detalle in detalles)
        {
            var detalleVenta = DetalleVenta.Crear(venta.Id, detalle.ProductoId, detalle.Cantidad, detalle.PrecioUnitario);
            _dbContext.DetallesVenta.Add(detalleVenta);
        }

        await _dbContext.SaveChangesAsync();

        await GenerarFacturaAsync(venta.Id, venta.Total);

        pedido.MarcarEntregado(venta.Id);
        await _dbContext.SaveChangesAsync();

        await transaction.CommitAsync();

        return (await ObtenerPedidoPorIdAsync(pedidoId))!;
    }

    /// <summary>
    /// Cancela el pedido: repone el stock de sus líneas "Disponible" (las "PorEncargo"
    /// nunca lo descontaron) y cancela los BackorderRequest pendientes que hubiera creado.
    /// </summary>
    public async Task<PedidoResponse> CancelarAsync(int pedidoId)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var pedido = await _dbContext.Pedidos.FirstOrDefaultAsync(p => p.Id == pedidoId);
        if (pedido == null)
            throw new PedidoNoEncontradoException(pedidoId);

        var detalles = await _dbContext.DetallesPedido.Where(d => d.PedidoId == pedidoId).ToListAsync();

        foreach (var detalle in detalles.Where(d => d.EstadoLinea == "Disponible"))
        {
            var producto = await _dbContext.Productos.FirstOrDefaultAsync(p => p.Id == detalle.ProductoId);
            if (producto == null)
                throw new ArgumentException($"El producto con ID {detalle.ProductoId} no existe.");

            producto.Reponer(detalle.Cantidad);
            _dbContext.MovimientosStock.Add(
                MovimientoStock.Crear(producto.Id, "Ajuste", detalle.Cantidad, "Pedido", pedido.Id));
        }

        var detalleIds = detalles.Select(d => d.Id).ToList();
        var backordersPendientes = await _dbContext.BackorderRequests
            .Where(b => b.DetallePedidoId != null
                        && detalleIds.Contains(b.DetallePedidoId.Value)
                        && b.Estado == "Pendiente")
            .ToListAsync();

        foreach (var backorder in backordersPendientes)
            backorder.Cancelar();

        pedido.Cancelar();

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConcurrencyException(
                "El stock de uno de los productos cambió mientras se procesaba la cancelación del pedido. Intenta de nuevo.");
        }

        await transaction.CommitAsync();

        return (await ObtenerPedidoPorIdAsync(pedidoId))!;
    }

    private async Task<PedidoResponse> EjecutarTransicionAsync(int pedidoId, Action<Pedido> transicion)
    {
        var pedido = await _dbContext.Pedidos.FirstOrDefaultAsync(p => p.Id == pedidoId);
        if (pedido == null)
            throw new PedidoNoEncontradoException(pedidoId);

        transicion(pedido);
        await _dbContext.SaveChangesAsync();

        return (await ObtenerPedidoPorIdAsync(pedidoId))!;
    }

    /// <summary>
    /// Genera y persiste la Factura de una venta: se guarda primero sin Numero para obtener
    /// el Id real asignado por SQL Server, y recién con ese Id se genera el correlativo
    /// definitivo. Misma lógica que VentaService.GenerarFacturaAsync.
    /// </summary>
    private async Task<Factura> GenerarFacturaAsync(int ventaId, decimal total)
    {
        var factura = Factura.Crear(ventaId, null, total);
        _dbContext.Facturas.Add(factura);
        await _dbContext.SaveChangesAsync();

        factura.GenerarNumero();
        await _dbContext.SaveChangesAsync();

        return factura;
    }

    private async Task<List<DetallePedidoResponse>> ObtenerDetallesPedidoAsync(int pedidoId)
    {
        return await _dbContext.DetallesPedido
            .AsNoTracking()
            .Where(d => d.PedidoId == pedidoId)
            .Join(
                _dbContext.Productos.AsNoTracking(),
                detalle => detalle.ProductoId,
                producto => producto.Id,
                (detalle, producto) => new DetallePedidoResponse(
                    detalle.Id,
                    detalle.ProductoId,
                    producto.Nombre,
                    detalle.Cantidad,
                    detalle.PrecioUnitario,
                    detalle.PrecioUnitario * detalle.Cantidad,
                    detalle.EstadoLinea))
            .ToListAsync();
    }

    private sealed record CalculoLineaPedido(Producto Producto, int Cantidad, decimal PrecioUnitario, bool Disponible);
}
