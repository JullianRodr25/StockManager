namespace StockManager.Application.DTOs;

/// <summary>
/// Modelo ÚNICO de lo que se imprime en una factura (tiquete térmico, vista digital y PDF).
/// Se arma una sola vez en el backend para que las tres presentaciones muestren exactamente
/// los mismos datos y las reglas (cliente final, datos faltantes, resumen de IVA) no se
/// dupliquen en cada cliente.
/// </summary>
public record FacturaDocumentoResponse(
    string Numero,
    DateTime Fecha,
    string? Vendedor,
    FacturaEmisorDto Emisor,
    FacturaCompradorDto Comprador,
    List<FacturaLineaDto> Lineas,
    List<FacturaResumenIvaDto> ResumenIva,
    decimal SubtotalBase,
    decimal TotalIva,
    decimal Total,
    List<DetallePagoResponse> Pagos,
    decimal? MontoRecibido,
    decimal? Cambio,
    string? TextoLegal,
    string? PoliticaCambios);

/// <summary>Datos del negocio tomados de la Configuración (nada va quemado en el código).</summary>
public record FacturaEmisorDto(
    string? Nombre,
    string? Nit,
    string? Direccion,
    string? Barrio,
    string? Ciudad,
    string? Telefono,
    string? Email,
    string? ResponsabilidadIva,
    string? ActividadEconomica,
    /// <summary>Frase ya armada de la resolución DIAN; null si Gold no la ha configurado.</summary>
    string? ResolucionDianTexto);

/// <summary>
/// Quién compra. Cliente no registrado → "CLIENTE FINAL" sin documento (no se guarda nada).
/// Cliente registrado → datos de la maestra de clientes, con banderas para avisar qué falta.
/// </summary>
public record FacturaCompradorDto(
    int? ClienteId,
    string Nombre,
    string? TipoDocumento,
    string? Documento,
    string? Direccion,
    string? Telefono,
    string? DireccionEntrega,
    bool EsClienteFinal,
    bool FaltaDocumento,
    bool FaltaDireccion);

public record FacturaLineaDto(
    string Producto,
    int Cantidad,
    decimal PrecioUnitario,
    decimal TarifaIva,
    decimal Total);

/// <summary>Una fila por tarifa de IVA distinta (0 %, 19 %, …): base gravable y valor del impuesto.</summary>
public record FacturaResumenIvaDto(decimal Tarifa, decimal Base, decimal Iva);
