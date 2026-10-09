using System.ComponentModel.DataAnnotations;

namespace StockManager.Application.DTOs;

public class ConfiguracionResponse
{
    public decimal TarifaIvaPorDefecto { get; set; }

    /// <summary>
    /// Número de WhatsApp (E.164) que recibe las alertas administrativas. Null si el aviso
    /// está desactivado. Solo se expone tal cual: no hay nada sensible que enmascarar, y
    /// tanto Admin como Empleado pueden consultarlo (igual que la tarifa de IVA).
    /// </summary>
    public string? TelefonoNotificacionesAdmin { get; set; }

    /// <summary>
    /// Nombre de la impresora térmica de tiquetes (tal como la ve QZ Tray/el sistema
    /// operativo) usada para imprimir la factura y abrir el cajón de dinero. Null si
    /// aún no se ha configurado.
    /// </summary>
    public string? NombreImpresoraTickets { get; set; }

    /// <summary>
    /// Datos del emisor (negocio) que se imprimen en el encabezado de la factura y del
    /// tiquete físico: razón social, NIT, dirección, teléfono y correo. Todos opcionales;
    /// null mientras un Admin no los haya configurado.
    /// </summary>
    public string? NombreEmpresa { get; set; }
    public string? NitEmpresa { get; set; }
    public string? DireccionEmpresa { get; set; }
    public string? TelefonoEmpresa { get; set; }
    public string? EmailEmpresa { get; set; }

    // Datos de facturación (impresos en el tiquete y la factura). Todos opcionales.
    public string? CiudadEmpresa { get; set; }
    public string? BarrioEmpresa { get; set; }
    public string? ResponsabilidadIvaEmpresa { get; set; }
    public string? ActividadEconomicaEmpresa { get; set; }
    public string? ResolucionDianNumero { get; set; }
    public DateTime? ResolucionDianFecha { get; set; }
    public string? ResolucionDianPrefijo { get; set; }
    public int? ResolucionDianRangoDesde { get; set; }
    public int? ResolucionDianRangoHasta { get; set; }
    public int? ResolucionDianVigenciaMeses { get; set; }
    public string? TextoLegalFactura { get; set; }
    public string? PoliticaCambiosFactura { get; set; }
}

public class ActualizarConfiguracionRequest
{
    [Range(0, 100)]
    public decimal TarifaIvaPorDefecto { get; set; }

    // El formato E.164 se valida en el dominio (Configuracion.ActualizarTelefonoNotificacionesAdmin)
    // en vez de con un atributo acá, porque el valor válido incluye null/"" (aviso desactivado)
    // y [RegularExpression] no maneja bien ese caso sin duplicar la lógica de "vacío = ok".
    public string? TelefonoNotificacionesAdmin { get; set; }

    public string? NombreImpresoraTickets { get; set; }

    public string? NombreEmpresa { get; set; }
    public string? NitEmpresa { get; set; }
    public string? DireccionEmpresa { get; set; }
    public string? TelefonoEmpresa { get; set; }
    public string? EmailEmpresa { get; set; }

    [MaxLength(100)] public string? CiudadEmpresa { get; set; }
    [MaxLength(100)] public string? BarrioEmpresa { get; set; }
    [MaxLength(100)] public string? ResponsabilidadIvaEmpresa { get; set; }
    [MaxLength(50)] public string? ActividadEconomicaEmpresa { get; set; }
    [MaxLength(50)] public string? ResolucionDianNumero { get; set; }
    public DateTime? ResolucionDianFecha { get; set; }
    [MaxLength(10)] public string? ResolucionDianPrefijo { get; set; }
    [Range(1, int.MaxValue)] public int? ResolucionDianRangoDesde { get; set; }
    [Range(1, int.MaxValue)] public int? ResolucionDianRangoHasta { get; set; }
    [Range(1, 120)] public int? ResolucionDianVigenciaMeses { get; set; }
    [MaxLength(600)] public string? TextoLegalFactura { get; set; }
    [MaxLength(300)] public string? PoliticaCambiosFactura { get; set; }
}
