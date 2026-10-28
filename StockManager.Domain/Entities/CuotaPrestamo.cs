namespace StockManager.Domain.Entities;

/// <summary>
/// Cuota de un préstamo que se descuenta en una quincena concreta. Se calcula al preparar la
/// quincena y solo se aplica al saldo del préstamo cuando la quincena se cierra.
/// </summary>
public class CuotaPrestamo
{
    public int Id { get; private set; }
    public int PeriodoNominaId { get; private set; }
    public int PrestamoId { get; private set; }

    /// <summary>Número de cuota dentro del préstamo (1, 2, 3...).</summary>
    public int Numero { get; private set; }

    public decimal Valor { get; private set; }
    public bool Aplicada { get; private set; }

    private CuotaPrestamo() { }

    public static CuotaPrestamo Crear(int periodoNominaId, int prestamoId, int numero, decimal valor)
    {
        if (valor <= 0)
            throw new ArgumentException("El valor de la cuota debe ser mayor que cero.", nameof(valor));

        return new CuotaPrestamo
        {
            PeriodoNominaId = periodoNominaId,
            PrestamoId = prestamoId,
            Numero = numero,
            Valor = valor
        };
    }

    public void MarcarAplicada() => Aplicada = true;
}
