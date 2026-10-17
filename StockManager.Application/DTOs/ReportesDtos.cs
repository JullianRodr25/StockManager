namespace StockManager.Application.DTOs;

// Todos los reportes son de solo lectura. Los rangos de fechas son DÍAS de calendario en
// Colombia (inclusivos); los instantes se convierten a UTC en el servicio.

public record VentasPorDiaItem(DateOnly Dia, int Cantidad, decimal Total);

public record VentasPorMetodoItem(string Metodo, int Cantidad, decimal Total);

public record ReporteVentasResponse(
    DateOnly Desde,
    DateOnly Hasta,
    decimal TotalVendido,
    int CantidadVentas,
    decimal TicketPromedio,
    decimal AbonosRecibidos,
    IReadOnlyList<VentasPorDiaItem> PorDia,
    IReadOnlyList<VentasPorMetodoItem> PorMetodo);

public record ProductoVendidoItem(
    int ProductoId,
    string Nombre,
    string Categoria,
    int Unidades,
    decimal Ingresos,
    decimal UtilidadEstimada);

public record ReporteProductosVendidosResponse(
    DateOnly Desde,
    DateOnly Hasta,
    IReadOnlyList<ProductoVendidoItem> Productos);

public record InventarioCategoriaItem(string Categoria, int Productos, int Unidades, decimal ValorCosto, decimal ValorVenta);

public record ProductoStockBajoItem(int ProductoId, string Nombre, string Categoria, int StockActual, int StockMinimo, int Faltante);

public record ReporteInventarioResponse(
    int ProductosActivos,
    int Agotados,
    int StockBajo,
    decimal ValorCosto,
    decimal ValorVenta,
    IReadOnlyList<InventarioCategoriaItem> PorCategoria,
    IReadOnlyList<ProductoStockBajoItem> ProductosStockBajo);

public record CuentaPorCobrarItem(int VentaId, string Cliente, DateTime Fecha, int DiasAbierta, decimal Total, decimal Saldo);

public record RangoAntiguedadItem(string Rango, int Cuentas, decimal Saldo);

public record SaldoProveedorItem(int ProveedorId, string Proveedor, int Cuentas, decimal Saldo, decimal SaldoVencido, DateTime? ProximoVencimiento);

public record ReporteCarteraResponse(
    decimal TotalPorCobrar,
    IReadOnlyList<RangoAntiguedadItem> AntiguedadPorCobrar,
    IReadOnlyList<CuentaPorCobrarItem> PorCobrar,
    decimal TotalPorPagar,
    decimal TotalVencidoPorPagar,
    IReadOnlyList<SaldoProveedorItem> PorPagar);
