using Microsoft.EntityFrameworkCore;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Entities;
using StockManager.Domain.Exceptions;
using StockManager.Infrastructure.Data;

namespace StockManager.Infrastructure.Services;

public class CuentaPorPagarService : ICuentaPorPagarService
{
    private readonly AppDbContext _dbContext;

    public CuentaPorPagarService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CuentaPorPagarResponse> CrearAsync(CrearCuentaPorPagarRequest request)
    {
        var proveedorExiste = await _dbContext.Proveedores.AnyAsync(p => p.Id == request.ProveedorId);
        if (!proveedorExiste)
            throw new ProveedorNoEncontradoException(request.ProveedorId);

        var cuenta = CuentaPorPagar.Crear(request.ProveedorId, request.Concepto, request.MontoTotal, request.FechaVencimiento);

        _dbContext.CuentasPorPagar.Add(cuenta);
        await _dbContext.SaveChangesAsync();

        return (await ObtenerPorIdAsync(cuenta.Id))!;
    }

    public async Task<CuentaPorPagarResponse> RegistrarAbonoAsync(int cuentaId, RegistrarAbonoCuentaPorPagarRequest request, int empleadoId)
    {
        if (request.Monto <= 0)
            throw new ArgumentException("El monto debe ser mayor a 0.");

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var cuenta = await _dbContext.CuentasPorPagar.FirstOrDefaultAsync(c => c.Id == cuentaId);
        if (cuenta == null)
            throw new CuentaPorPagarNoEncontradaException(cuentaId);

        if (cuenta.Estado != "Pendiente")
            throw new CuentaPorPagarEstadoInvalidoException(cuenta.Id, cuenta.Estado, "Pendiente");

        var totalAbonado = await _dbContext.AbonosCuentaPorPagar
            .Where(a => a.CuentaPorPagarId == cuentaId)
            .SumAsync(a => (decimal?)a.Monto) ?? 0m;

        var saldoPendiente = cuenta.MontoTotal - totalAbonado;

        if (request.Monto > saldoPendiente)
            throw new ArgumentException(
                $"El monto ({request.Monto:F2}) no puede ser mayor al saldo pendiente ({saldoPendiente:F2}).");

        var abono = AbonoCuentaPorPagar.Crear(cuentaId, request.Monto, request.MetodoPago, empleadoId);
        _dbContext.AbonosCuentaPorPagar.Add(abono);

        var nuevoSaldo = saldoPendiente - request.Monto;
        if (nuevoSaldo == 0)
            cuenta.MarcarPagada();

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return (await ObtenerPorIdAsync(cuenta.Id))!;
    }

    public async Task<CuentaPorPagarResponse> CancelarAsync(int cuentaId)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var cuenta = await _dbContext.CuentasPorPagar.FirstOrDefaultAsync(c => c.Id == cuentaId);
        if (cuenta == null)
            throw new CuentaPorPagarNoEncontradaException(cuentaId);

        var tieneAbonos = await _dbContext.AbonosCuentaPorPagar.AnyAsync(a => a.CuentaPorPagarId == cuentaId);
        if (tieneAbonos)
            throw new OperacionInvalidaCuentaPorPagarException(
                $"La cuenta por pagar {cuentaId} no se puede cancelar porque ya tiene abonos registrados.");

        cuenta.Cancelar();
        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return (await ObtenerPorIdAsync(cuenta.Id))!;
    }

    public async Task<CuentaPorPagarResponse?> ObtenerPorIdAsync(int id)
    {
        var cuenta = await _dbContext.CuentasPorPagar.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (cuenta == null)
            return null;

        var proveedorNombre = await _dbContext.Proveedores
            .AsNoTracking()
            .Where(p => p.Id == cuenta.ProveedorId)
            .Select(p => p.Nombre)
            .FirstOrDefaultAsync() ?? string.Empty;

        var abonos = await _dbContext.AbonosCuentaPorPagar
            .AsNoTracking()
            .Where(a => a.CuentaPorPagarId == id)
            .OrderBy(a => a.Fecha)
            .Select(a => new AbonoCuentaPorPagarResponse(a.Id, a.Monto, a.MetodoPago, a.Fecha, a.EmpleadoId))
            .ToListAsync();

        var saldoPendiente = cuenta.MontoTotal - abonos.Sum(a => a.Monto);
        var vencida = EsVencida(cuenta.Estado, cuenta.FechaVencimiento);

        return new CuentaPorPagarResponse(
            cuenta.Id,
            cuenta.ProveedorId,
            proveedorNombre,
            cuenta.Concepto,
            cuenta.MontoTotal,
            saldoPendiente,
            cuenta.FechaCompra,
            cuenta.FechaVencimiento,
            cuenta.Estado,
            vencida,
            abonos);
    }

    public async Task<(List<CuentaPorPagarResumenResponse> Items, int Total)> ObtenerPaginadoAsync(
        int pagina, int tamanoPagina, string? estado, int? proveedorId)
    {
        var query = _dbContext.CuentasPorPagar.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(estado))
            query = query.Where(c => c.Estado == estado);

        if (proveedorId.HasValue)
            query = query.Where(c => c.ProveedorId == proveedorId.Value);

        var total = await query.CountAsync();

        var cuentasPagina = await query
            .OrderBy(c => c.FechaVencimiento)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .Join(
                _dbContext.Proveedores.AsNoTracking(),
                cuenta => cuenta.ProveedorId,
                proveedor => proveedor.Id,
                (cuenta, proveedor) => new { Cuenta = cuenta, ProveedorNombre = proveedor.Nombre })
            .ToListAsync();

        var items = await MapearResumenesAsync(cuentasPagina.Select(x => (x.Cuenta, x.ProveedorNombre)));

        return (items, total);
    }

    public async Task<List<CuentaPorPagarResumenResponse>> ObtenerProximasAVencerAsync(int dias)
    {
        var limite = DateTime.UtcNow.Date.AddDays(dias);

        var cuentas = await _dbContext.CuentasPorPagar
            .AsNoTracking()
            .Where(c => c.Estado == "Pendiente" && c.FechaVencimiento <= limite)
            .Join(
                _dbContext.Proveedores.AsNoTracking(),
                cuenta => cuenta.ProveedorId,
                proveedor => proveedor.Id,
                (cuenta, proveedor) => new { Cuenta = cuenta, ProveedorNombre = proveedor.Nombre })
            .OrderBy(x => x.Cuenta.FechaVencimiento)
            .ToListAsync();

        return await MapearResumenesAsync(cuentas.Select(x => (x.Cuenta, x.ProveedorNombre)));
    }

    private async Task<List<CuentaPorPagarResumenResponse>> MapearResumenesAsync(IEnumerable<(CuentaPorPagar Cuenta, string ProveedorNombre)> cuentas)
    {
        var lista = cuentas.ToList();
        var idsCuentas = lista.Select(x => x.Cuenta.Id).ToList();

        var abonosPorCuenta = await _dbContext.AbonosCuentaPorPagar
            .AsNoTracking()
            .Where(a => idsCuentas.Contains(a.CuentaPorPagarId))
            .GroupBy(a => a.CuentaPorPagarId)
            .Select(g => new { CuentaPorPagarId = g.Key, TotalAbonado = g.Sum(a => a.Monto) })
            .ToDictionaryAsync(x => x.CuentaPorPagarId, x => x.TotalAbonado);

        return lista.Select(x =>
        {
            var totalAbonado = abonosPorCuenta.GetValueOrDefault(x.Cuenta.Id, 0m);
            var saldoPendiente = x.Cuenta.MontoTotal - totalAbonado;
            var vencida = EsVencida(x.Cuenta.Estado, x.Cuenta.FechaVencimiento);

            return new CuentaPorPagarResumenResponse(
                x.Cuenta.Id,
                x.Cuenta.ProveedorId,
                x.ProveedorNombre,
                x.Cuenta.Concepto,
                x.Cuenta.MontoTotal,
                saldoPendiente,
                x.Cuenta.FechaVencimiento,
                x.Cuenta.Estado,
                vencida);
        }).ToList();
    }

    /// <summary>
    /// "Vencida" es un estado derivado, no persistido: una cuenta Pendiente cuya fecha de
    /// vencimiento ya pasó. Evita que un job tenga que mantener sincronizado un flag aparte.
    /// </summary>
    private static bool EsVencida(string estado, DateTime fechaVencimiento) =>
        estado == "Pendiente" && fechaVencimiento.Date < DateTime.UtcNow.Date;
}
