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
}

public class ActualizarConfiguracionRequest
{
    [Range(0, 100)]
    public decimal TarifaIvaPorDefecto { get; set; }

    // El formato E.164 se valida en el dominio (Configuracion.ActualizarTelefonoNotificacionesAdmin)
    // en vez de con un atributo acá, porque el valor válido incluye null/"" (aviso desactivado)
    // y [RegularExpression] no maneja bien ese caso sin duplicar la lógica de "vacío = ok".
    public string? TelefonoNotificacionesAdmin { get; set; }
}