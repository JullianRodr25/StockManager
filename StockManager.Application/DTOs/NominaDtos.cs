namespace StockManager.Application.DTOs;

// ---------- Trabajadores ----------

public record GuardarTrabajadorRequest(
    string Nombre,
    string NumeroIdentificacion,
    string? Telefono,
    string Cargo,
    string TipoContrato,
    DateTime FechaIngreso,
    DateTime? FechaFinContrato,
    decimal SalarioMensual,
    bool AplicaSeguridadSocial,
    string MetodoPago,
    // Semanal | Quincenal | Mensual. Si no se envía, Quincenal.
    string FrecuenciaPago = "Quincenal"
);

public record TrabajadorResponse(
    int Id,
    string Nombre,
    string NumeroIdentificacion,
    string? Telefono,
    string Cargo,
    string TipoContrato,
    DateTime FechaIngreso,
    DateTime? FechaFinContrato,
    decimal SalarioMensual,
    bool AplicaSeguridadSocial,
    string MetodoPago,
    string FrecuenciaPago,
    bool Activo,
    // Lo que todavía debe de préstamos y adelantos activos.
    decimal SaldoPrestamos,
    // Días que faltan para que termine el contrato (negativo si ya venció); null si no tiene fecha de cierre.
    int? DiasParaFinContrato
);

// ---------- Parámetros ----------

public record ParametrosNominaRequest(
    decimal SalarioMinimo,
    decimal AuxilioTransporte,
    decimal LimiteSalariosAuxilio,
    decimal PorcentajeSaludTrabajador,
    decimal PorcentajePensionTrabajador
);

public record ParametrosNominaResponse(
    decimal SalarioMinimo,
    decimal AuxilioTransporte,
    decimal LimiteSalariosAuxilio,
    decimal PorcentajeSaludTrabajador,
    decimal PorcentajePensionTrabajador,
    DateTime FechaActualizacion
);

// ---------- Préstamos ----------

public record CrearPrestamoRequest(
    int TrabajadorId,
    string Tipo,
    decimal Monto,
    int NumeroCuotas,
    // Cualquier día de la quincena desde la que se empieza a descontar.
    DateTime PrimeraCuotaDesde,
    string? Nota
);

public record PrestamoResponse(
    int Id,
    int TrabajadorId,
    string NombreTrabajador,
    string Tipo,
    decimal Monto,
    int NumeroCuotas,
    decimal ValorCuota,
    DateTime PrimeraCuotaDesde,
    int CuotasDescontadas,
    decimal SaldoPendiente,
    string Estado,
    string? Nota,
    DateTime FechaCreacion,
    // Lo que se descontaría en la próxima quincena.
    decimal ProximaCuota
);

// ---------- Quincenas ----------

public record AbrirPeriodoNominaRequest(
    // Si no se envía, se abre el período siguiente al último de esa frecuencia (o el actual si no hay ninguno).
    DateTime? FechaInicio,
    // Semanal | Quincenal | Mensual. Si no se envía, Quincenal.
    string? Frecuencia = null
);

public record AgregarNovedadRequest(
    int TrabajadorId,
    string Tipo,
    string Concepto,
    decimal Valor
);

public record NovedadNominaResponse(
    int Id,
    int TrabajadorId,
    string Tipo,
    string Concepto,
    decimal Valor
);

public record CuotaNominaResponse(
    int PrestamoId,
    string Tipo,
    int Numero,
    int NumeroCuotas,
    decimal Valor
);

public record LiquidacionNominaResponse(
    int Id,
    int TrabajadorId,
    string NombreTrabajador,
    string Cargo,
    string MetodoPago,
    decimal SalarioQuincena,
    decimal AuxilioTransporte,
    decimal OtrosDevengos,
    decimal Devengado,
    decimal DescuentoSalud,
    decimal DescuentoPension,
    decimal OtrosDescuentos,
    decimal DescuentoPrestamos,
    decimal TotalDescuentos,
    decimal Neto,
    bool Pagada,
    DateTime? PagadaEn,
    IReadOnlyList<NovedadNominaResponse> Novedades,
    IReadOnlyList<CuotaNominaResponse> Cuotas
);

public record PeriodoNominaResumenResponse(
    int Id,
    string Frecuencia,
    DateTime FechaInicio,
    DateTime FechaFin,
    string Estado,
    int NumeroTrabajadores,
    decimal TotalDevengado,
    decimal TotalSeguridadSocial,
    decimal TotalPrestamos,
    decimal TotalOtrosDescuentos,
    decimal TotalNeto,
    DateTime? AprobadaEn,
    DateTime? PagadaEn,
    DateTime? CerradaEn
);

public record PeriodoNominaResponse(
    PeriodoNominaResumenResponse Resumen,
    IReadOnlyList<LiquidacionNominaResponse> Liquidaciones
);

// ---------- Panel e inicio ----------

public record AlertaNominaResponse(
    // contrato_vencido | contrato_por_vencer | cuotas_proximas | prima | quincena_sin_abrir
    string Tipo,
    // info | advertencia | critica
    string Severidad,
    string Titulo,
    string Detalle,
    int? TrabajadorId
);

public record NominaResumenResponse(
    int TrabajadoresActivos,
    decimal NominaMensualEstimada,
    decimal SaldoPrestamosPendiente,
    int PrestamosActivos,
    PeriodoNominaResumenResponse? PeriodoActual,
    IReadOnlyList<AlertaNominaResponse> Alertas
);
