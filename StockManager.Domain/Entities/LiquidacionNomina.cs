using StockManager.Domain.Common;
namespace StockManager.Domain.Entities;

/// <summary>
/// Lo que se le liquida a un trabajador en una quincena. Guarda una copia (snapshot) de su
/// nombre, cargo y método de pago: al aprobar la quincena el documento no cambia aunque
/// después se edite el trabajador.
/// </summary>
public class LiquidacionNomina
{
    public int Id { get; private set; }
    public int PeriodoNominaId { get; private set; }
    public int TrabajadorId { get; private set; }

    public string NombreTrabajador { get; private set; } = null!;
    public string Cargo { get; private set; } = null!;
    public string MetodoPago { get; private set; } = null!;

    public decimal SalarioQuincena { get; private set; }
    public decimal AuxilioTransporte { get; private set; }
    public decimal OtrosDevengos { get; private set; }
    public decimal Devengado { get; private set; }

    public decimal DescuentoSalud { get; private set; }
    public decimal DescuentoPension { get; private set; }
    public decimal OtrosDescuentos { get; private set; }
    public decimal DescuentoPrestamos { get; private set; }

    public decimal Neto { get; private set; }

    public bool Pagada { get; private set; }
    public DateTime? PagadaEn { get; private set; }

    /// <summary>Turno de caja del que salió el efectivo; solo las liquidaciones pagadas en efectivo lo llevan.</summary>
    public int? TurnoCajaId { get; private set; }

    private LiquidacionNomina() { }

    public decimal SeguridadSocial => DescuentoSalud + DescuentoPension;
    public decimal TotalDescuentos => SeguridadSocial + OtrosDescuentos + DescuentoPrestamos;

    public static LiquidacionNomina Crear(int periodoNominaId, Trabajador trabajador, ResultadoLiquidacion resultado)
    {
        var liquidacion = new LiquidacionNomina
        {
            PeriodoNominaId = periodoNominaId,
            TrabajadorId = trabajador.Id
        };
        liquidacion.Actualizar(trabajador, resultado);
        return liquidacion;
    }

    /// <summary>Vuelve a calcular la liquidación (solo mientras la quincena es borrador).</summary>
    public void Actualizar(Trabajador trabajador, ResultadoLiquidacion resultado)
    {
        NombreTrabajador = trabajador.Nombre;
        Cargo = trabajador.Cargo;
        MetodoPago = trabajador.MetodoPago;

        SalarioQuincena = resultado.SalarioQuincena;
        AuxilioTransporte = resultado.AuxilioTransporte;
        OtrosDevengos = resultado.OtrosDevengos;
        Devengado = resultado.Devengado;
        DescuentoSalud = resultado.DescuentoSalud;
        DescuentoPension = resultado.DescuentoPension;
        OtrosDescuentos = resultado.OtrosDescuentos;
        DescuentoPrestamos = resultado.DescuentoPrestamos;
        Neto = resultado.Neto;
    }

    public void MarcarPagada(int? turnoCajaId)
    {
        Pagada = true;
        PagadaEn = DateTime.UtcNow;
        TurnoCajaId = MetodoPago == Trabajador.PagoEfectivo ? turnoCajaId : null;
    }
}
