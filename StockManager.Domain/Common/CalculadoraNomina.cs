using StockManager.Domain.Entities;

namespace StockManager.Domain.Common;

/// <summary>Resultado del cálculo de la quincena de un trabajador, en pesos enteros.</summary>
public sealed record ResultadoLiquidacion(
    decimal SalarioQuincena,
    decimal AuxilioTransporte,
    decimal OtrosDevengos,
    decimal Devengado,
    decimal DescuentoSalud,
    decimal DescuentoPension,
    decimal OtrosDescuentos,
    decimal DescuentoPrestamos,
    decimal Neto)
{
    /// <summary>
    /// Lo máximo que se puede descontar por préstamos sin dejar el neto en negativo.
    /// </summary>
    public decimal CapacidadParaPrestamos => Math.Max(0m, Neto);

    /// <summary>Aplica el descuento de préstamos y recalcula el neto.</summary>
    public ResultadoLiquidacion ConPrestamos(decimal descuentoPrestamos) =>
        this with
        {
            DescuentoPrestamos = descuentoPrestamos,
            Neto = Devengado - DescuentoSalud - DescuentoPension - OtrosDescuentos - descuentoPrestamos
        };
}

/// <summary>
/// Fórmula del período. Es una función pura (sin base de datos) a propósito: es dinero, y así
/// se puede revisar y probar sin infraestructura. Todo se redondea a pesos enteros.
/// </summary>
public static class CalculadoraNomina
{
    public static decimal Redondear(decimal valor) =>
        Math.Round(valor, 0, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Parte de un valor mensual que corresponde al período: mensual = completo, quincenal = la
    /// mitad y semanal = suma de sus 7 días, donde cada día vale el valor dividido entre los días
    /// reales de SU mes (febrero 28, octubre 31). Así una semana que cruza dos meses queda exacta.
    /// </summary>
    public static decimal ValorDelPeriodo(decimal valorMensual, PeriodoNomina periodo) =>
        periodo.Frecuencia switch
        {
            Trabajador.FrecuenciaMensual => valorMensual,
            Trabajador.FrecuenciaSemanal => Enumerable.Range(0, 7)
                .Select(i => periodo.FechaInicio.AddDays(i))
                .Sum(dia => valorMensual / DateTime.DaysInMonth(dia.Year, dia.Month)),
            _ => valorMensual / 2m
        };

    /// <summary>
    /// Calcula la quincena sin préstamos (el neto resultante es el disponible para descontarlos).
    /// - Salario del período según la frecuencia (ver ValorDelPeriodo).
    /// - Auxilio de transporte (misma proporción) si gana hasta el límite de salarios mínimos y no es
    ///   prestación de servicios.
    /// - Salud y pensión del trabajador = porcentaje sobre el salario (no sobre el auxilio), solo
    ///   si el trabajador tiene activada la seguridad social.
    /// </summary>
    public static ResultadoLiquidacion Calcular(
        Trabajador trabajador,
        ParametrosNomina parametros,
        decimal otrosDevengos,
        decimal otrosDescuentos,
        PeriodoNomina periodo)
    {
        var salario = Redondear(ValorDelPeriodo(trabajador.SalarioMensual, periodo));

        var recibeAuxilio =
            trabajador.TipoContrato != Trabajador.ContratoPrestacionServicios
            && trabajador.SalarioMensual <= parametros.LimiteSalariosAuxilio * parametros.SalarioMinimo;
        var auxilio = recibeAuxilio ? Redondear(ValorDelPeriodo(parametros.AuxilioTransporte, periodo)) : 0m;

        var salud = trabajador.AplicaSeguridadSocial
            ? Redondear(salario * parametros.PorcentajeSaludTrabajador / 100m)
            : 0m;
        var pension = trabajador.AplicaSeguridadSocial
            ? Redondear(salario * parametros.PorcentajePensionTrabajador / 100m)
            : 0m;

        var devengado = salario + auxilio + otrosDevengos;

        return new ResultadoLiquidacion(
            salario, auxilio, otrosDevengos, devengado,
            salud, pension, otrosDescuentos, 0m,
            devengado - salud - pension - otrosDescuentos);
    }
}
