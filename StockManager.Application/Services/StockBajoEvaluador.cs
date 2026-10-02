namespace StockManager.Application.Services;

/// <summary>
/// Lógica pura (sin IO) para decidir si un Producto debe entrar o salir de la alerta de
/// "stock bajo". Centralizada acá para que el chequeo periódico de respaldo
/// (NotificacionesStockBajoCheckService) y el chequeo instantáneo (disparado justo después de
/// cualquier cambio de stock ya confirmado — ver StockBajoInstantaneoStockNotificador) usen
/// exactamente el mismo criterio sin duplicar la condición en dos lugares.
/// </summary>
public static class StockBajoEvaluador
{
    public readonly record struct Resultado(bool DebeNotificar, bool DebeLimpiarBandera);

    /// <summary>
    /// Producto.NotificacionStockBajoActiva evita reabrir la misma notificación mientras el
    /// stock sigue bajo: solo se genera una vez por "episodio" (hasta que se repone por
    /// encima del mínimo).
    /// </summary>
    public static Resultado Evaluar(int stockActual, int stockMinimo, bool notificacionActiva)
    {
        var enStockBajo = stockActual <= stockMinimo;
        return new Resultado(
            DebeNotificar: enStockBajo && !notificacionActiva,
            DebeLimpiarBandera: !enStockBajo && notificacionActiva);
    }
}
