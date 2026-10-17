using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Entities;
using StockManager.Domain.Exceptions;
using StockManager.Infrastructure.Data;

namespace StockManager.Infrastructure.Services;

public class TurnoCajaService : ITurnoCajaService
{
    // Denominaciones que se cuentan en el cierre (billetes y monedas de peso colombiano). La
    // última fila de la pantalla agrupa las monedas de 200 o menos, valoradas a 200 cada una.
    private static readonly int[] DenominacionesValidas = { 100000, 50000, 20000, 10000, 5000, 2000, 1000, 500, 200 };

    private const string Efectivo = "Efectivo";
    private const string Tarjeta = "Tarjeta";
    private const string Transferencia = "Transferencia";

    private readonly AppDbContext _dbContext;

    public TurnoCajaService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TurnoCajaResponse> AbrirAsync(int empleadoId, AbrirTurnoRequest request)
    {
        var yaTieneAbierto = await _dbContext.TurnosCaja
            .AnyAsync(t => t.EmpleadoId == empleadoId && t.Estado == TurnoCaja.EstadoAbierto);
        if (yaTieneAbierto)
            throw new TurnoCajaYaAbiertoException();

        var turno = TurnoCaja.Abrir(empleadoId, request.BaseInicial);
        _dbContext.TurnosCaja.Add(turno);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Dos aperturas simultáneas: el índice único filtrado dejó pasar solo una.
            throw new TurnoCajaYaAbiertoException();
        }

        return await MapearAsync(turno, esperadoVisible: true);
    }

    public async Task<TurnoCajaResponse?> ObtenerTurnoActualAsync(int empleadoId, bool esAdmin)
    {
        var turno = await _dbContext.TurnosCaja
            .FirstOrDefaultAsync(t => t.EmpleadoId == empleadoId && t.Estado == TurnoCaja.EstadoAbierto);
        if (turno is null)
            return null;

        return await MapearAsync(turno, await EsperadoVisibleAsync(esAdmin));
    }

    public async Task<TurnoCajaResponse> CerrarAsync(int empleadoId, CerrarTurnoRequest request)
    {
        var turno = await _dbContext.TurnosCaja
            .FirstOrDefaultAsync(t => t.EmpleadoId == empleadoId && t.Estado == TurnoCaja.EstadoAbierto);
        if (turno is null)
            throw new OperacionInvalidaCajaException("No tienes un turno de caja abierto.");

        return await CerrarInternoAsync(turno, empleadoId, request);
    }

    public async Task<TurnoCajaResponse> ForzarCierreAsync(int turnoId, int adminId, CerrarTurnoRequest request)
    {
        var turno = await _dbContext.TurnosCaja.FirstOrDefaultAsync(t => t.Id == turnoId);
        if (turno is null)
            throw new TurnoCajaNoEncontradoException(turnoId);

        return await CerrarInternoAsync(turno, adminId, request);
    }

    public async Task<TurnoCajaResponse> ObtenerPorIdAsync(int turnoId, int usuarioId, bool esAdmin)
    {
        var turno = await _dbContext.TurnosCaja.FirstOrDefaultAsync(t => t.Id == turnoId);

        // Un empleado que pide el turno de otro recibe "no encontrado": no se le revela que existe.
        if (turno is null || (!esAdmin && turno.EmpleadoId != usuarioId))
            throw new TurnoCajaNoEncontradoException(turnoId);

        var visible = turno.Estado == TurnoCaja.EstadoCerrado || await EsperadoVisibleAsync(esAdmin);
        return await MapearAsync(turno, visible);
    }

    public async Task<(List<TurnoCajaResumenResponse> Items, int Total)> ObtenerHistorialAsync(
        int usuarioId, bool esAdmin, int pagina, int tamanoPagina)
    {
        if (pagina < 1) pagina = 1;
        if (tamanoPagina < 1) tamanoPagina = 20;
        if (tamanoPagina > 100) tamanoPagina = 100;

        var consulta = _dbContext.TurnosCaja.AsNoTracking()
            .Where(t => esAdmin || t.EmpleadoId == usuarioId);

        var total = await consulta.CountAsync();

        var turnos = await consulta
            .OrderByDescending(t => t.FechaApertura)
            .Skip((pagina - 1) * tamanoPagina)
            .Take(tamanoPagina)
            .ToListAsync();

        var empleadoIds = turnos.Select(t => t.EmpleadoId).Distinct().ToList();
        var nombres = await _dbContext.Empleados.AsNoTracking()
            .Where(e => empleadoIds.Contains(e.Id))
            .ToDictionaryAsync(e => e.Id, e => e.Nombre);

        var items = turnos
            .Select(t => new TurnoCajaResumenResponse(
                t.Id,
                t.EmpleadoId,
                nombres.GetValueOrDefault(t.EmpleadoId, string.Empty),
                t.FechaApertura,
                t.FechaCierre,
                t.Estado,
                t.BaseInicial,
                t.EfectivoEsperado,
                t.EfectivoContado,
                t.Diferencia))
            .ToList();

        return (items, total);
    }

    public async Task<int?> ResolverTurnoParaPagoAsync(int empleadoId, bool incluyeEfectivo)
    {
        var turnoId = await _dbContext.TurnosCaja
            .Where(t => t.EmpleadoId == empleadoId && t.Estado == TurnoCaja.EstadoAbierto)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync();

        if (turnoId is null && incluyeEfectivo)
            throw new TurnoCajaNoAbiertoException();

        return turnoId;
    }

    // ---------------------------------------------------------------------------------------

    private async Task<TurnoCajaResponse> CerrarInternoAsync(TurnoCaja turno, int cerradoPorEmpleadoId, CerrarTurnoRequest request)
    {
        var conteo = NormalizarConteo(request.Conteo);
        var contado = conteo.Sum(c => (decimal)c.Denominacion * c.Cantidad);

        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        var totales = await CalcularTotalesAsync(turno.Id);

        turno.Cerrar(
            contado,
            request.Nota,
            totales,
            cerradoPorEmpleadoId,
            JsonSerializer.Serialize(conteo));

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();

        return await MapearAsync(turno, esperadoVisible: true);
    }

    /// <summary>
    /// Valida que solo se cuenten denominaciones conocidas, junta las repetidas y descarta las
    /// filas en cero. El total contado se calcula siempre en el servidor, nunca se confía en uno
    /// enviado por el cliente.
    /// </summary>
    private static List<ConteoDenominacionRequest> NormalizarConteo(List<ConteoDenominacionRequest>? conteo)
    {
        if (conteo is null)
            throw new OperacionInvalidaCajaException("Debes enviar el conteo del efectivo.");

        foreach (var linea in conteo)
        {
            if (!DenominacionesValidas.Contains(linea.Denominacion))
                throw new OperacionInvalidaCajaException($"La denominación {linea.Denominacion} no es válida.");

            if (linea.Cantidad < 0)
                throw new OperacionInvalidaCajaException("La cantidad contada no puede ser negativa.");
        }

        return conteo
            .GroupBy(c => c.Denominacion)
            .Select(g => new ConteoDenominacionRequest { Denominacion = g.Key, Cantidad = g.Sum(c => c.Cantidad) })
            .Where(c => c.Cantidad > 0)
            .OrderByDescending(c => c.Denominacion)
            .ToList();
    }

    private async Task<bool> EsperadoVisibleAsync(bool esAdmin)
    {
        if (esAdmin)
            return true;

        var conteoCiego = await _dbContext.Configuraciones
            .Select(c => c.ConteoCiegoCaja)
            .FirstOrDefaultAsync();

        return !conteoCiego;
    }

    /// <summary>
    /// Suma lo que pasó por el turno. Las ventas pagadas cuentan por su total (no por lo recibido:
    /// el cambio no se queda en caja) y las de pago mixto se reparten según su desglose.
    /// </summary>
    private async Task<TotalesTurnoCaja> CalcularTotalesAsync(int turnoId)
    {
        var ventasDirectas = await _dbContext.Ventas
            .Where(v => v.TurnoCajaId == turnoId
                        && v.Estado == "Pagada"
                        && !v.EsCotizacion
                        && v.MetodoPago != "Mixto")
            .GroupBy(v => v.MetodoPago)
            .Select(g => new { Metodo = g.Key, Monto = g.Sum(v => v.Total) })
            .ToListAsync();

        var ventasMixtas = await _dbContext.DetallesPagoVenta
            .Where(d => _dbContext.Ventas.Any(v => v.Id == d.VentaId
                                                   && v.TurnoCajaId == turnoId
                                                   && v.Estado == "Pagada"
                                                   && !v.EsCotizacion
                                                   && v.MetodoPago == "Mixto"))
            .GroupBy(d => d.MetodoPago)
            .Select(g => new { Metodo = g.Key, Monto = g.Sum(d => d.Monto) })
            .ToListAsync();

        var abonosFiado = await _dbContext.AbonosCuenta
            .Where(a => a.TurnoCajaId == turnoId)
            .GroupBy(a => a.MetodoPago)
            .Select(g => new { Metodo = g.Key, Monto = g.Sum(a => a.Monto) })
            .ToListAsync();

        var pagosProveedorEfectivo = await _dbContext.AbonosCuentaPorPagar
            .Where(a => a.TurnoCajaId == turnoId && a.MetodoPago == Efectivo)
            .SumAsync(a => (decimal?)a.Monto) ?? 0m;

        var entradas = new List<(string? Metodo, decimal Monto)>();
        entradas.AddRange(ventasDirectas.Select(x => (Metodo: (string?)x.Metodo, Monto: x.Monto)));
        entradas.AddRange(ventasMixtas.Select(x => (Metodo: (string?)x.Metodo, Monto: x.Monto)));

        var abonos = abonosFiado
            .Select(x => (Metodo: (string?)x.Metodo, Monto: x.Monto))
            .ToList();

        static decimal Sumar(IEnumerable<(string? Metodo, decimal Monto)> origen, string metodo) =>
            origen.Where(x => x.Metodo == metodo).Sum(x => x.Monto);

        return new TotalesTurnoCaja(
            VentasEfectivo: Sumar(entradas, Efectivo),
            AbonosFiadoEfectivo: Sumar(abonos, Efectivo),
            PagosProveedorEfectivo: pagosProveedorEfectivo,
            Transferencias: Sumar(entradas, Transferencia) + Sumar(abonos, Transferencia),
            Tarjeta: Sumar(entradas, Tarjeta) + Sumar(abonos, Tarjeta));
    }

    private async Task<TurnoCajaResponse> MapearAsync(TurnoCaja turno, bool esperadoVisible)
    {
        var cerrado = turno.Estado == TurnoCaja.EstadoCerrado;
        var conteoCiego = await _dbContext.Configuraciones
            .Select(c => c.ConteoCiegoCaja)
            .FirstOrDefaultAsync();

        var empleadoId = turno.EmpleadoId;
        var cerradoPorId = turno.CerradoPorEmpleadoId;
        var nombres = await _dbContext.Empleados.AsNoTracking()
            .Where(e => e.Id == empleadoId || e.Id == cerradoPorId)
            .ToDictionaryAsync(e => e.Id, e => e.Nombre);

        TotalesCajaResponse? totalesResponse = null;
        decimal? esperado = null;

        if (cerrado)
        {
            var t = turno.TotalesCierre!;
            totalesResponse = new TotalesCajaResponse(
                t.VentasEfectivo, t.AbonosFiadoEfectivo, t.PagosProveedorEfectivo, t.Transferencias, t.Tarjeta);
            esperado = turno.EfectivoEsperado;
        }
        else if (esperadoVisible)
        {
            var t = await CalcularTotalesAsync(turno.Id);
            totalesResponse = new TotalesCajaResponse(
                t.VentasEfectivo, t.AbonosFiadoEfectivo, t.PagosProveedorEfectivo, t.Transferencias, t.Tarjeta);
            esperado = t.EfectivoEsperado(turno.BaseInicial);
        }

        List<ConteoDenominacionResponse>? conteo = null;
        if (cerrado && !string.IsNullOrWhiteSpace(turno.DetalleConteo))
        {
            var guardado = JsonSerializer.Deserialize<List<ConteoDenominacionRequest>>(turno.DetalleConteo) ?? new();
            conteo = guardado
                .Select(c => new ConteoDenominacionResponse(c.Denominacion, c.Cantidad, (decimal)c.Denominacion * c.Cantidad))
                .ToList();
        }

        return new TurnoCajaResponse(
            turno.Id,
            turno.EmpleadoId,
            nombres.GetValueOrDefault(turno.EmpleadoId, string.Empty),
            turno.FechaApertura,
            turno.BaseInicial,
            turno.Estado,
            conteoCiego,
            cerrado || esperadoVisible,
            totalesResponse,
            esperado,
            turno.FechaCierre,
            turno.CerradoPorEmpleadoId.HasValue ? nombres.GetValueOrDefault(turno.CerradoPorEmpleadoId.Value) : null,
            turno.EfectivoContado,
            turno.Diferencia,
            turno.NotaCierre,
            conteo);
    }
}
