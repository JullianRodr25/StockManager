namespace StockManager.Domain.Entities;

/// <summary>
/// Persona que trabaja en la ferretería y se controla en la nómina, con los datos de su
/// contrato vigente. No es lo mismo que Empleado (usuario del sistema): un trabajador puede
/// no tener acceso al sistema, y por eso son entidades separadas.
/// El historial de contratos y de cambios de sueldo se agregará cuando la nómina lo requiera.
/// </summary>
public class Trabajador
{
    public const string ContratoIndefinido = "Indefinido";
    public const string ContratoFijo = "Fijo";
    public const string ContratoObraLabor = "ObraLabor";
    public const string ContratoPrestacionServicios = "PrestacionServicios";

    public const string PagoEfectivo = "Efectivo";
    public const string PagoTransferencia = "Transferencia";

    public static readonly string[] TiposContrato =
    [
        ContratoIndefinido, ContratoFijo, ContratoObraLabor, ContratoPrestacionServicios
    ];

    public static readonly string[] MetodosPago = [PagoEfectivo, PagoTransferencia];

    public int Id { get; private set; }
    public string Nombre { get; private set; } = null!;
    public string NumeroIdentificacion { get; private set; } = null!;
    public string? Telefono { get; private set; }
    public string Cargo { get; private set; } = null!;
    public string TipoContrato { get; private set; } = null!;

    /// <summary>Día de calendario (no instante) en que empezó a trabajar.</summary>
    public DateTime FechaIngreso { get; private set; }

    /// <summary>Día en que termina el contrato; solo aplica a contratos con fecha de cierre.</summary>
    public DateTime? FechaFinContrato { get; private set; }

    public decimal SalarioMensual { get; private set; }

    /// <summary>Si se le descuentan y controlan salud y pensión. Se decide por trabajador.</summary>
    public bool AplicaSeguridadSocial { get; private set; }

    /// <summary>Cómo se le paga normalmente (Efectivo o Transferencia).</summary>
    public string MetodoPago { get; private set; } = PagoEfectivo;

    public bool Activo { get; private set; }
    public DateTime FechaCreacion { get; private set; }

    private Trabajador() { }

    public static Trabajador Crear(
        string nombre, string numeroIdentificacion, string? telefono, string cargo,
        string tipoContrato, DateTime fechaIngreso, DateTime? fechaFinContrato,
        decimal salarioMensual, bool aplicaSeguridadSocial, string metodoPago)
    {
        var trabajador = new Trabajador { Activo = true, FechaCreacion = DateTime.UtcNow };
        trabajador.Actualizar(nombre, numeroIdentificacion, telefono, cargo, tipoContrato,
            fechaIngreso, fechaFinContrato, salarioMensual, aplicaSeguridadSocial, metodoPago);
        return trabajador;
    }

    public void Actualizar(
        string nombre, string numeroIdentificacion, string? telefono, string cargo,
        string tipoContrato, DateTime fechaIngreso, DateTime? fechaFinContrato,
        decimal salarioMensual, bool aplicaSeguridadSocial, string metodoPago)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del trabajador no puede estar vacío.", nameof(nombre));
        if (nombre.Trim().Length > 200)
            throw new ArgumentException("El nombre no puede exceder 200 caracteres.", nameof(nombre));

        if (string.IsNullOrWhiteSpace(numeroIdentificacion))
            throw new ArgumentException("El número de identificación es obligatorio.", nameof(numeroIdentificacion));
        if (numeroIdentificacion.Trim().Length > 50)
            throw new ArgumentException("El número de identificación no puede exceder 50 caracteres.", nameof(numeroIdentificacion));

        if (string.IsNullOrWhiteSpace(cargo))
            throw new ArgumentException("El cargo es obligatorio.", nameof(cargo));
        if (cargo.Trim().Length > 100)
            throw new ArgumentException("El cargo no puede exceder 100 caracteres.", nameof(cargo));

        if (!string.IsNullOrWhiteSpace(telefono) && telefono.Trim().Length > 20)
            throw new ArgumentException("El teléfono no puede exceder 20 caracteres.", nameof(telefono));

        if (!TiposContrato.Contains(tipoContrato))
            throw new ArgumentException("El tipo de contrato no es válido.", nameof(tipoContrato));

        if (!MetodosPago.Contains(metodoPago))
            throw new ArgumentException("El método de pago no es válido.", nameof(metodoPago));

        if (salarioMensual <= 0)
            throw new ArgumentException("El salario debe ser mayor que cero.", nameof(salarioMensual));

        var ingreso = fechaIngreso.Date;
        var fin = fechaFinContrato?.Date;

        // Un contrato a término fijo o por obra necesita saber cuándo termina para poder avisar.
        if ((tipoContrato == ContratoFijo || tipoContrato == ContratoObraLabor) && fin is null)
            throw new ArgumentException("Este tipo de contrato necesita fecha de finalización.", nameof(fechaFinContrato));
        if (fin is not null && fin < ingreso)
            throw new ArgumentException("La fecha de finalización no puede ser anterior a la de ingreso.", nameof(fechaFinContrato));

        Nombre = nombre.Trim();
        NumeroIdentificacion = numeroIdentificacion.Trim();
        Telefono = string.IsNullOrWhiteSpace(telefono) ? null : telefono.Trim();
        Cargo = cargo.Trim();
        TipoContrato = tipoContrato;
        FechaIngreso = ingreso;
        FechaFinContrato = fin;
        SalarioMensual = salarioMensual;
        AplicaSeguridadSocial = aplicaSeguridadSocial;
        MetodoPago = metodoPago;
    }

    public void Desactivar() => Activo = false;
    public void Activar() => Activo = true;
}
