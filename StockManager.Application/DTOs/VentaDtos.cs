namespace StockManager.Application.DTOs;

public record LineaVentaRequest(int ProductoId, int Cantidad);

/// <summary>
/// Una línea del desglose de un pago "Mixto": cuánto de la venta se pagó con este método
/// individual. Solo tiene sentido cuando el MetodoPago de la venta/cierre es "Mixto" — para
/// cualquier otro método no debe enviarse (ver Venta.ValidarDetallesPago).
/// </summary>
public record DetallePagoRequest(string MetodoPago, decimal Monto);

public record DetallePagoResponse(string MetodoPago, decimal Monto);

public record RegistrarVentaRequest(
    int? ClienteId,
    string? NombreComprador,
    string? TelefonoComprador,
    string? EmailComprador,
    string MetodoPago,
    List<LineaVentaRequest> Lineas,
    decimal? MontoRecibido = null,
    List<DetallePagoRequest>? DetallesPago = null,
    // true si el comprador pidió factura electrónica en esta venta puntual (no es un dato
    // del Cliente: el mismo cliente puede pedirla unas veces y otras no). Si viene true y no
    // se informan los campos fiscales de abajo, VentaService intenta completarlos con los
    // datos guardados en el perfil del Cliente (requiere ClienteId); si tampoco los tiene,
    // la venta se rechaza.
    bool RequiereFacturaElectronica = false,
    // Datos fiscales de ESTA venta: se usan tal cual si vienen informados (ej. el cliente
    // quiere facturar a nombre de una empresa distinta a su perfil, o es un comprador nuevo
    // que se registra en el momento), o quedan null para que se completen desde el Cliente.
    string? TipoDocumentoFiscal = null,
    string? NumeroDocumentoFiscal = null,
    string? RazonSocialFiscal = null,
    string? DireccionFiscal = null,
    string? EmailFacturacion = null
);

public record DetalleVentaResponse(
    int Id,
    int ProductoId,
    string ProductoNombre,
    int Cantidad,
    decimal PrecioUnitario,
    decimal SubtotalSinIva,
    decimal Iva,
    decimal SubtotalConIva
);

public record VentaResponse(
    int Id,
    int? ClienteId,
    string? NombreComprador,
    string? TelefonoComprador,
    string? EmailComprador,
    string? MetodoPago,
    int EmpleadoId,
    DateTime Fecha,
    string Estado,
    decimal Total,
    string NumeroFactura,
    List<DetalleVentaResponse> Detalles,
    decimal? MontoRecibido = null,
    decimal? Cambio = null,
    List<DetallePagoResponse>? DetallesPago = null,
    bool RequiereFacturaElectronica = false,
    string EstadoFacturaElectronica = "NoAplica",
    string? TipoDocumentoFacturado = null,
    string? NumeroDocumentoFacturado = null,
    string? RazonSocialFacturada = null,
    string? DireccionFacturada = null,
    string? EmailFacturacion = null
);

public record VentaResumenResponse(
    int Id,
    string? NombreComprador,
    int? ClienteId,
    DateTime Fecha,
    string Estado,
    decimal Total,
    string? MetodoPago,
    string NumeroFactura
);

public record AbrirFiadoRequest(int ClienteId);

public record CerrarFiadoRequest(string MetodoPago, decimal? MontoRecibido = null, List<DetallePagoRequest>? DetallesPago = null);

public record RegistrarAbonoRequest(decimal Monto, string MetodoPago);

public record AbonoResponse(
    int Id,
    int VentaId,
    decimal Monto,
    string MetodoPago,
    DateTime Fecha,
    int EmpleadoId
);

public record EditarCantidadLineaRequest(int Cantidad);
