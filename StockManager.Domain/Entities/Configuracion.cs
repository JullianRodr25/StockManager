namespace StockManager.Domain.Entities;

public class Configuracion
{
    public int Id { get; private set; }
    public decimal TarifaIvaPorDefecto { get; private set; }

    private Configuracion() { }

    public static Configuracion Crear(decimal tarifaIvaPorDefecto)
    {
        if (tarifaIvaPorDefecto < 0 || tarifaIvaPorDefecto > 100)
            throw new ArgumentException("La tarifa de IVA debe estar entre 0 y 100.");

        return new Configuracion { TarifaIvaPorDefecto = tarifaIvaPorDefecto };
    }

    public void ActualizarTarifaIva(decimal nuevaTarifa)
    {
        if (nuevaTarifa < 0 || nuevaTarifa > 100)
            throw new ArgumentException("La tarifa de IVA debe estar entre 0 y 100.");

        TarifaIvaPorDefecto = nuevaTarifa;
    }
}