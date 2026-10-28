namespace StockManager.Domain.Entities;

/// <summary>
/// Valores legales y porcentajes que usa el cálculo de la nómina. Se guardan en base de datos
/// (una sola fila) y no en el código porque cambian cada año y deben poder corregirse sin
/// desplegar. Los valores iniciales son una referencia: confírmalos con el contador.
/// </summary>
public class ParametrosNomina
{
    public int Id { get; private set; }

    /// <summary>Salario mínimo mensual vigente (SMMLV).</summary>
    public decimal SalarioMinimo { get; private set; }

    /// <summary>Auxilio de transporte mensual vigente.</summary>
    public decimal AuxilioTransporte { get; private set; }

    /// <summary>El auxilio aplica a quien gana hasta este número de salarios mínimos.</summary>
    public decimal LimiteSalariosAuxilio { get; private set; }

    /// <summary>Porcentaje de salud que se descuenta al trabajador sobre su salario.</summary>
    public decimal PorcentajeSaludTrabajador { get; private set; }

    /// <summary>Porcentaje de pensión que se descuenta al trabajador sobre su salario.</summary>
    public decimal PorcentajePensionTrabajador { get; private set; }

    public DateTime FechaActualizacion { get; private set; }

    private ParametrosNomina() { }

    /// <summary>Valores de referencia para 2026 (SMMLV y auxilio de transporte).</summary>
    public static ParametrosNomina CrearPorDefecto() =>
        new()
        {
            SalarioMinimo = 1_750_905m,
            AuxilioTransporte = 249_095m,
            LimiteSalariosAuxilio = 2m,
            PorcentajeSaludTrabajador = 4m,
            PorcentajePensionTrabajador = 4m,
            FechaActualizacion = DateTime.UtcNow
        };

    public void Actualizar(
        decimal salarioMinimo, decimal auxilioTransporte, decimal limiteSalariosAuxilio,
        decimal porcentajeSalud, decimal porcentajePension)
    {
        if (salarioMinimo <= 0)
            throw new ArgumentException("El salario mínimo debe ser mayor que cero.", nameof(salarioMinimo));
        if (auxilioTransporte < 0)
            throw new ArgumentException("El auxilio de transporte no puede ser negativo.", nameof(auxilioTransporte));
        if (limiteSalariosAuxilio < 0)
            throw new ArgumentException("El límite de salarios del auxilio no puede ser negativo.", nameof(limiteSalariosAuxilio));
        if (porcentajeSalud < 0 || porcentajeSalud > 100)
            throw new ArgumentException("El porcentaje de salud debe estar entre 0 y 100.", nameof(porcentajeSalud));
        if (porcentajePension < 0 || porcentajePension > 100)
            throw new ArgumentException("El porcentaje de pensión debe estar entre 0 y 100.", nameof(porcentajePension));

        SalarioMinimo = salarioMinimo;
        AuxilioTransporte = auxilioTransporte;
        LimiteSalariosAuxilio = limiteSalariosAuxilio;
        PorcentajeSaludTrabajador = porcentajeSalud;
        PorcentajePensionTrabajador = porcentajePension;
        FechaActualizacion = DateTime.UtcNow;
    }
}
