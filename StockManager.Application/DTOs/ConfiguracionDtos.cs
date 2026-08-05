using System.ComponentModel.DataAnnotations;

namespace StockManager.Application.DTOs;

public class ConfiguracionResponse
{
    public decimal TarifaIvaPorDefecto { get; set; }
}

public class ActualizarConfiguracionRequest
{
    [Range(0, 100)]
    public decimal TarifaIvaPorDefecto { get; set; }
}