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
}