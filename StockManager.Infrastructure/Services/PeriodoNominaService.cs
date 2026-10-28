using Microsoft.EntityFrameworkCore;
using StockManager.Application.DTOs;
using StockManager.Application.Services;
using StockManager.Domain.Common;
using StockManager.Domain.Entities;
using StockManager.Domain.Exceptions;
using StockManager.Infrastructure.Data;

namespace StockManager.Infrastructure.Services;

public class PeriodoNominaService : IPeriodoNominaService
{
    private readonly AppDbContext _dbContext;
    private readonly IParametrosNominaService _parametrosService;
    private readonly ITurnoCajaService _turnoCajaService;

    // Proveedor que recibe las obligaciones de seguridad social en Cuentas por pagar.
    private const string ProveedorSeguridadSocial = "Seguridad social (aportes)";

    public PeriodoNominaService(
        AppDbContext dbContext,
        IParametrosNominaService parametrosService,
        ITurnoCajaService turnoCajaService)
    {
        _dbContext = dbContext;
        _parametrosService = parametrosService;
        _turnoCajaService = turnoCajaService;
    }

    // ---------- Consultas ----------

    public async Task<IReadOnlyList<PeriodoNominaResumenResponse>> ListarAsync()
    {
        var periodos = await _dbContext.PeriodosNomina
            .AsNoTracking()
            .OrderByDescending(p => p.FechaInicio)
            .ToListAsync();

        var totales = await ObtenerTotalesAsync(periodos.Select(p => p.Id).ToList());
        return periodos
            .Select(p => MapearResumen(p, totales.GetValueOrDefault(p.Id)))
            .ToList();
    }

    public async Task<PeriodoNominaResponse?> ObtenerActualAsync()
    {
        var inicio = PeriodoNomina.QuincenaDe(HoraColombia.Hoy()).Inicio;
        var periodo = await _dbContext.PeriodosNomina
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.FechaInicio == inicio);

        return periodo is null ? null : await ConstruirRespuestaAsync(periodo);
    }

    public async Task<PeriodoNominaResponse> ObtenerAsync(int id)
    {
        var periodo = await BuscarAsync(id, rastrear: false);
        return await ConstruirRespuestaAsync(periodo);
    }

    // ---------- Flujo de la quincena ----------

    public async Task<PeriodoNominaResponse> AbrirAsync(AbrirPeriodoNominaRequest request)
    {
        var inicio = await ResolverInicioAsync(request.FechaInicio);

        // Valida de entrada que sea una quincena válida (empieza el día 1 o el 16).
        var periodo = PeriodoNomina.Crear(inicio);
        var fin = periodo.FechaFin;

        if (await _dbContext.PeriodosNomina.AnyAsync(p => p.FechaInicio == inicio))
            throw new PeriodoNominaDuplicadoException(inicio, fin);

        // Los saldos de los préstamos solo son exactos si la quincena anterior ya se cerró.
        var anteriorSinCerrar = await _dbContext.PeriodosNomina
            .AnyAsync(p => p.FechaInicio < inicio && p.Estado != PeriodoNomina.EstadoCerrada);
        if (anteriorSinCerrar)
            throw new PeriodoNominaEstadoInvalidoException(
                "Cierra la quincena anterior antes de abrir la siguiente.");

        var posterior = await _dbContext.PeriodosNomina.AnyAsync(p => p.FechaInicio > inicio);
        if (posterior)
            throw new PeriodoNominaEstadoInvalidoException(
                "No se puede abrir una quincena anterior a una que ya existe.");

        _dbContext.PeriodosNomina.Add(periodo);

        try
        {
            await _dbContext.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Dos aperturas simultáneas de la misma quincena: el índice único dejó pasar solo una.
            throw new PeriodoNominaDuplicadoException(inicio, fin);
        }

        await ReconstruirLiquidacionesAsync(periodo);
        return await ConstruirRespuestaAsync(periodo);
    }

    public async Task<PeriodoNominaResponse> RecalcularAsync(int id)
    {
        var periodo = await BuscarAsync(id, rastrear: true);
        periodo.ExigirBorrador();

        await ReconstruirLiquidacionesAsync(periodo);
        return await ConstruirRespuestaAsync(periodo);
    }

    public async Task<PeriodoNominaResponse> AprobarAsync(int id, int empleadoId)
    {
        var periodo = await BuscarAsync(id, rastrear: true);
        periodo.ExigirBorrador();

        // Se recalcula justo antes de congelar para aprobar con los datos más recientes.
        await ReconstruirLiquidacionesAsync(periodo);

        var liquidaciones = await _dbContext.LiquidacionesNomina
            .Where(l => l.PeriodoNominaId == id)
            .ToListAsync();
        if (liquidaciones.Count == 0)
            throw new PeriodoNominaEstadoInvalidoException("No hay trabajadores para liquidar en esta quincena.");

        var negativo = liquidaciones.FirstOrDefault(l => l.Neto < 0);
        if (negativo is not null)
            throw new PeriodoNominaEstadoInvalidoException(
                $"El neto de {negativo.NombreTrabajador} es negativo. Revisa sus descuentos antes de aprobar.");

        periodo.Aprobar(empleadoId);
        await _dbContext.SaveChangesAsync();
        return await ConstruirRespuestaAsync(periodo);
    }

    public async Task<PeriodoNominaResponse> PagarLiquidacionAsync(int id, int liquidacionId, int empleadoId)
    {
        var periodo = await BuscarAsync(id, rastrear: true);
        ExigirAprobada(periodo);

        var liquidacion = await _dbContext.LiquidacionesNomina
            .FirstOrDefaultAsync(l => l.Id == liquidacionId && l.PeriodoNominaId == id)
            ?? throw new PeriodoNominaEstadoInvalidoException("La liquidación no pertenece a esta quincena.");

        if (!liquidacion.Pagada)
        {
            // El sueldo en efectivo sale del cajón: debe quedar en el turno abierto de quien paga.
            var turnoId = await _turnoCajaService.ResolverTurnoParaPagoAsync(
                empleadoId, liquidacion.MetodoPago == Trabajador.PagoEfectivo);
            liquidacion.MarcarPagada(turnoId);
        }

        await _dbContext.SaveChangesAsync();
        await MarcarPagadaSiCompletaAsync(periodo);
        return await ConstruirRespuestaAsync(periodo);
    }

    public async Task<PeriodoNominaResponse> PagarTodasAsync(int id, int empleadoId)
    {
        var periodo = await BuscarAsync(id, rastrear: true);
        ExigirAprobada(periodo);

        var pendientes = await _dbContext.LiquidacionesNomina
            .Where(l => l.PeriodoNominaId == id && !l.Pagada)
            .ToListAsync();
        var turnoId = await _turnoCajaService.ResolverTurnoParaPagoAsync(
            empleadoId, pendientes.Any(l => l.MetodoPago == Trabajador.PagoEfectivo));
        foreach (var liquidacion in pendientes)
            liquidacion.MarcarPagada(turnoId);

        await _dbContext.SaveChangesAsync();
        await MarcarPagadaSiCompletaAsync(periodo);
        return await ConstruirRespuestaAsync(periodo);
    }

    public async Task<PeriodoNominaResponse> CerrarAsync(int id)
    {
        var periodo = await BuscarAsync(id, rastrear: true);

        // Cerrar toca varias tablas (préstamos, cuentas por pagar): todo o nada.
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        periodo.Cerrar();

        // Recién ahora las cuotas se descuentan del saldo de cada préstamo.
        var cuotas = await _dbContext.CuotasPrestamo
            .Where(c => c.PeriodoNominaId == id && !c.Aplicada)
            .ToListAsync();
        var prestamoIds = cuotas.Select(c => c.PrestamoId).Distinct().ToList();
        var prestamos = await _dbContext.Prestamos
            .Where(p => prestamoIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        foreach (var cuota in cuotas)
        {
            prestamos[cuota.PrestamoId].RegistrarDescuento(cuota.Valor);
            cuota.MarcarAplicada();
        }

        await RegistrarObligacionSeguridadSocialAsync(periodo);

        await _dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
        return await ConstruirRespuestaAsync(periodo);
    }

    // ---------- Novedades ----------

    public async Task<PeriodoNominaResponse> AgregarNovedadAsync(int id, AgregarNovedadRequest request)
    {
        var periodo = await BuscarAsync(id, rastrear: true);
        periodo.ExigirBorrador();

        var participa = await _dbContext.LiquidacionesNomina
            .AnyAsync(l => l.PeriodoNominaId == id && l.TrabajadorId == request.TrabajadorId);
        if (!participa)
            throw new PeriodoNominaEstadoInvalidoException("El trabajador no está en esta quincena.");

        var novedad = NovedadNomina.Crear(id, request.TrabajadorId, request.Tipo, request.Concepto, request.Valor);
        _dbContext.NovedadesNomina.Add(novedad);
        await _dbContext.SaveChangesAsync();

        await ReconstruirLiquidacionesAsync(periodo);
        return await ConstruirRespuestaAsync(periodo);
    }

    public async Task<PeriodoNominaResponse> EliminarNovedadAsync(int id, int novedadId)
    {
        var periodo = await BuscarAsync(id, rastrear: true);
        periodo.ExigirBorrador();

        var novedad = await _dbContext.NovedadesNomina
            .FirstOrDefaultAsync(n => n.Id == novedadId && n.PeriodoNominaId == id)
            ?? throw new PeriodoNominaEstadoInvalidoException("La novedad no pertenece a esta quincena.");

        _dbContext.NovedadesNomina.Remove(novedad);
        await _dbContext.SaveChangesAsync();

        await ReconstruirLiquidacionesAsync(periodo);
        return await ConstruirRespuestaAsync(periodo);
    }

    public async Task RecalcularBorradoresAsync()
    {
        var borradores = await _dbContext.PeriodosNomina
            .Where(p => p.Estado == PeriodoNomina.EstadoBorrador)
            .ToListAsync();

        foreach (var periodo in borradores)
            await ReconstruirLiquidacionesAsync(periodo);
    }

    // ---------- Cálculo ----------

    /// <summary>
    /// Deja las liquidaciones y cuotas de una quincena en borrador según el estado actual de los
    /// trabajadores, las novedades y los préstamos. Es idempotente: se puede repetir sin efectos
    /// acumulados, porque borra y vuelve a generar todo.
    /// </summary>
    private async Task ReconstruirLiquidacionesAsync(PeriodoNomina periodo)
    {
        var parametros = await _parametrosService.ObtenerEntidadAsync();

        // Elegibles: activos, ya ingresados y con contrato vigente en algún día de la quincena.
        // No se prorratea: quien está en la quincena cobra la quincena completa.
        var trabajadores = await _dbContext.Trabajadores
            .Where(t => t.Activo
                && t.FechaIngreso <= periodo.FechaFin
                && (t.FechaFinContrato == null || t.FechaFinContrato >= periodo.FechaInicio))
            .OrderBy(t => t.Nombre)
            .ThenBy(t => t.Id)
            .ToListAsync();

        var novedades = await _dbContext.NovedadesNomina
            .Where(n => n.PeriodoNominaId == periodo.Id)
            .ToListAsync();

        var prestamos = await _dbContext.Prestamos
            .Where(p => p.Estado == Prestamo.EstadoActivo && p.PrimeraCuotaDesde <= periodo.FechaInicio)
            .OrderBy(p => p.FechaCreacion)
            .ThenBy(p => p.Id)
            .ToListAsync();

        var cuotasViejas = await _dbContext.CuotasPrestamo
            .Where(c => c.PeriodoNominaId == periodo.Id)
            .ToListAsync();
        var liquidacionesViejas = await _dbContext.LiquidacionesNomina
            .Where(l => l.PeriodoNominaId == periodo.Id)
            .ToListAsync();

        // Las novedades de quien ya no participa quedarían huérfanas e invisibles: se descartan.
        var idsElegibles = trabajadores.Select(t => t.Id).ToHashSet();
        var novedadesHuerfanas = novedades.Where(n => !idsElegibles.Contains(n.TrabajadorId)).ToList();
        _dbContext.NovedadesNomina.RemoveRange(novedadesHuerfanas);
        novedades = novedades.Except(novedadesHuerfanas).ToList();

        _dbContext.CuotasPrestamo.RemoveRange(cuotasViejas);
        _dbContext.LiquidacionesNomina.RemoveRange(liquidacionesViejas);

        // Primero se borra lo viejo y luego se inserta lo nuevo: los índices únicos
        // (quincena + trabajador, quincena + préstamo) no admiten ambas filas a la vez.
        await _dbContext.SaveChangesAsync();

        foreach (var trabajador in trabajadores)
        {
            var susNovedades = novedades.Where(n => n.TrabajadorId == trabajador.Id).ToList();
            var otrosDevengos = susNovedades.Where(n => n.Tipo == NovedadNomina.TipoDevengo).Sum(n => n.Valor);
            var otrosDescuentos = susNovedades.Where(n => n.Tipo == NovedadNomina.TipoDescuento).Sum(n => n.Valor);

            var resultado = CalculadoraNomina.Calcular(trabajador, parametros, otrosDevengos, otrosDescuentos);

            // Las cuotas solo pueden quitar lo que sobra: el neto nunca queda negativo por un préstamo.
            var disponible = resultado.CapacidadParaPrestamos;
            var descuentoPrestamos = 0m;
            foreach (var prestamo in prestamos.Where(p => p.TrabajadorId == trabajador.Id))
            {
                var valor = Math.Min(prestamo.CuotaPendiente(), disponible);
                if (valor <= 0)
                    continue;

                _dbContext.CuotasPrestamo.Add(CuotaPrestamo.Crear(
                    periodo.Id, prestamo.Id, prestamo.CuotasDescontadas + 1, valor));
                disponible -= valor;
                descuentoPrestamos += valor;
            }

            _dbContext.LiquidacionesNomina.Add(LiquidacionNomina.Crear(
                periodo.Id, trabajador, resultado.ConPrestamos(descuentoPrestamos)));
        }

        await _dbContext.SaveChangesAsync();
    }

    // ---------- Apoyo ----------

    private async Task<PeriodoNomina> BuscarAsync(int id, bool rastrear)
    {
        var consulta = rastrear ? _dbContext.PeriodosNomina : _dbContext.PeriodosNomina.AsNoTracking();
        return await consulta.FirstOrDefaultAsync(p => p.Id == id)
            ?? throw new PeriodoNominaNoEncontradoException(id);
    }

    /// <summary>
    /// Deja en Cuentas por pagar lo retenido a los trabajadores por salud y pensión en la quincena,
    /// para que se pague a las entidades. Vence el día 10 del mes siguiente (fecha de referencia).
    /// No incluye los aportes a cargo del empleador, que la nómina todavía no calcula.
    /// </summary>
    private async Task RegistrarObligacionSeguridadSocialAsync(PeriodoNomina periodo)
    {
        var total = await _dbContext.LiquidacionesNomina
            .Where(l => l.PeriodoNominaId == periodo.Id)
            .SumAsync(l => (decimal?)(l.DescuentoSalud + l.DescuentoPension)) ?? 0m;
        if (total <= 0)
            return;

        var proveedorId = await _dbContext.Proveedores
            .Where(p => p.Nombre == ProveedorSeguridadSocial)
            .Select(p => (int?)p.Id)
            .FirstOrDefaultAsync();

        if (proveedorId is null)
        {
            var proveedor = Proveedor.Crear(ProveedorSeguridadSocial, null, null, null, null);
            _dbContext.Proveedores.Add(proveedor);
            await _dbContext.SaveChangesAsync();
            proveedorId = proveedor.Id;
        }

        // Día 10 del mes siguiente al fin de la quincena.
        var vencimiento = new DateTime(periodo.FechaFin.Year, periodo.FechaFin.Month, 1).AddMonths(1).AddDays(9);

        var concepto = $"Aportes de seguridad social retenidos, quincena {periodo.FechaInicio:dd/MM/yyyy} al {periodo.FechaFin:dd/MM/yyyy}";
        _dbContext.CuentasPorPagar.Add(CuentaPorPagar.Crear(proveedorId.Value, concepto, total, vencimiento));
    }

    private static void ExigirAprobada(PeriodoNomina periodo)
    {
        if (periodo.Estado != PeriodoNomina.EstadoAprobada)
            throw new PeriodoNominaEstadoInvalidoException(
                $"Solo se registran pagos en una quincena aprobada; esta está {periodo.Estado.ToLowerInvariant()}.");
    }

    private async Task MarcarPagadaSiCompletaAsync(PeriodoNomina periodo)
    {
        var faltan = await _dbContext.LiquidacionesNomina
            .AnyAsync(l => l.PeriodoNominaId == periodo.Id && !l.Pagada);
        if (faltan)
            return;

        periodo.MarcarPagada();
        await _dbContext.SaveChangesAsync();
    }

    private async Task<DateTime> ResolverInicioAsync(DateTime? fechaInicio)
    {
        if (fechaInicio.HasValue)
            return fechaInicio.Value.Date;

        var ultima = await _dbContext.PeriodosNomina.MaxAsync(p => (DateTime?)p.FechaInicio);
        return ultima.HasValue
            ? PeriodoNomina.QuincenaSiguiente(ultima.Value).Inicio
            : PeriodoNomina.QuincenaDe(HoraColombia.Hoy()).Inicio;
    }

    private sealed record TotalesPeriodo(
        int Trabajadores, decimal Devengado, decimal SeguridadSocial,
        decimal Prestamos, decimal OtrosDescuentos, decimal Neto);

    private async Task<Dictionary<int, TotalesPeriodo>> ObtenerTotalesAsync(IReadOnlyCollection<int> periodoIds)
    {
        var filas = await _dbContext.LiquidacionesNomina
            .AsNoTracking()
            .Where(l => periodoIds.Contains(l.PeriodoNominaId))
            .GroupBy(l => l.PeriodoNominaId)
            .Select(g => new
            {
                PeriodoId = g.Key,
                Trabajadores = g.Count(),
                Devengado = g.Sum(l => l.Devengado),
                Salud = g.Sum(l => l.DescuentoSalud),
                Pension = g.Sum(l => l.DescuentoPension),
                Prestamos = g.Sum(l => l.DescuentoPrestamos),
                OtrosDescuentos = g.Sum(l => l.OtrosDescuentos),
                Neto = g.Sum(l => l.Neto)
            })
            .ToListAsync();

        return filas.ToDictionary(
            f => f.PeriodoId,
            f => new TotalesPeriodo(
                f.Trabajadores, f.Devengado, f.Salud + f.Pension,
                f.Prestamos, f.OtrosDescuentos, f.Neto));
    }

    private static PeriodoNominaResumenResponse MapearResumen(PeriodoNomina p, TotalesPeriodo? t) => new(
        p.Id,
        p.FechaInicio,
        p.FechaFin,
        p.Estado,
        t?.Trabajadores ?? 0,
        t?.Devengado ?? 0m,
        t?.SeguridadSocial ?? 0m,
        t?.Prestamos ?? 0m,
        t?.OtrosDescuentos ?? 0m,
        t?.Neto ?? 0m,
        p.AprobadaEn,
        p.PagadaEn,
        p.CerradaEn);

    private async Task<PeriodoNominaResponse> ConstruirRespuestaAsync(PeriodoNomina periodo)
    {
        var liquidaciones = await _dbContext.LiquidacionesNomina
            .AsNoTracking()
            .Where(l => l.PeriodoNominaId == periodo.Id)
            .OrderBy(l => l.NombreTrabajador)
            .ThenBy(l => l.Id)
            .ToListAsync();

        var novedades = await _dbContext.NovedadesNomina
            .AsNoTracking()
            .Where(n => n.PeriodoNominaId == periodo.Id)
            .OrderBy(n => n.Id)
            .ToListAsync();

        var cuotas = await (
            from c in _dbContext.CuotasPrestamo.AsNoTracking()
            join p in _dbContext.Prestamos.AsNoTracking() on c.PrestamoId equals p.Id
            where c.PeriodoNominaId == periodo.Id
            select new { p.TrabajadorId, c.PrestamoId, p.Tipo, c.Numero, p.NumeroCuotas, c.Valor })
            .ToListAsync();

        var filas = liquidaciones.Select(l => new LiquidacionNominaResponse(
            l.Id,
            l.TrabajadorId,
            l.NombreTrabajador,
            l.Cargo,
            l.MetodoPago,
            l.SalarioQuincena,
            l.AuxilioTransporte,
            l.OtrosDevengos,
            l.Devengado,
            l.DescuentoSalud,
            l.DescuentoPension,
            l.OtrosDescuentos,
            l.DescuentoPrestamos,
            l.TotalDescuentos,
            l.Neto,
            l.Pagada,
            l.PagadaEn,
            novedades
                .Where(n => n.TrabajadorId == l.TrabajadorId)
                .Select(n => new NovedadNominaResponse(n.Id, n.TrabajadorId, n.Tipo, n.Concepto, n.Valor))
                .ToList(),
            cuotas
                .Where(c => c.TrabajadorId == l.TrabajadorId)
                .Select(c => new CuotaNominaResponse(c.PrestamoId, c.Tipo, c.Numero, c.NumeroCuotas, c.Valor))
                .ToList()))
            .ToList();

        var totales = new TotalesPeriodo(
            filas.Count,
            filas.Sum(f => f.Devengado),
            filas.Sum(f => f.DescuentoSalud + f.DescuentoPension),
            filas.Sum(f => f.DescuentoPrestamos),
            filas.Sum(f => f.OtrosDescuentos),
            filas.Sum(f => f.Neto));

        return new PeriodoNominaResponse(MapearResumen(periodo, totales), filas);
    }
}
