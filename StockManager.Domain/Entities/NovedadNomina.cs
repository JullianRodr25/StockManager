namespace StockManager.Domain.Entities;

/// <summary>
/// Ajuste de una quincena para un trabajador: un devengo (bono, horas extra) que suma o un
/// descuento (falta, otro) que resta. Solo se carga mientras la quincena es borrador.
/// </summary>
public class NovedadNomina
{
    public const string TipoDevengo = "Devengo";
    public const string TipoDescuento = "Descuento";

    public int Id { get; private set; }
    public int PeriodoNominaId { get; private set; }
    public int TrabajadorId { get; private set; }
    public string Tipo { get; private set; } = null!;
    public string Concepto { get; private set; } = null!;
    public decimal Valor { get; private set; }
    public DateTime FechaCreacion { get; private set; }

    private NovedadNomina() { }

    public static NovedadNomina Crear(int periodoNominaId, int trabajadorId, string tipo, string concepto, decimal valor)
    {
        if (tipo != TipoDevengo && tipo != TipoDescuento)
            throw new ArgumentException("El tipo debe ser devengo o descuento.", nameof(tipo));
        if (string.IsNullOrWhiteSpace(concepto))
            throw new ArgumentException("El concepto es obligatorio.", nameof(concepto));
        if (concepto.Trim().Length > 100)
            throw new ArgumentException("El concepto no puede exceder 100 caracteres.", nameof(concepto));
        if (valor <= 0)
            throw new ArgumentException("El valor debe ser mayor que cero.", nameof(valor));

        return new NovedadNomina
        {
            PeriodoNominaId = periodoNominaId,
            TrabajadorId = trabajadorId,
            Tipo = tipo,
            Concepto = concepto.Trim(),
            Valor = valor,
            FechaCreacion = DateTime.UtcNow
        };
    }
}
