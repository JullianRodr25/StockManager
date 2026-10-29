using System.ComponentModel.DataAnnotations;

namespace StockManager.Application.DTOs;

public class AbrirTurnoRequest
{
    /// <summary>
    /// Efectivo con el que arranca la caja (la "base"). Solo se usa cuando no hay ninguna caja
    /// cerrada; si la hay, la base es el efectivo contado en el último cierre y este valor se ignora.
    /// </summary>
    [Range(0, 1_000_000_000)]
    public decimal BaseInicial { get; set; }
}

/// <summary>Lo que necesita la pantalla para abrir caja: con qué base arranca y cuál fue el último cierre.</summary>
public record AperturaCajaResponse(
    // true cuando no hay ninguna caja cerrada: solo entonces la base se escribe a mano.
    bool RequiereBaseManual,
    // Efectivo contado en el último cierre (0 si no hay).
    decimal BaseSugerida,
    TurnoCajaResponse? UltimoCierre);

public class VerificarContrasenaRequest
{
    [System.ComponentModel.DataAnnotations.Required]
    public string Contrasena { get; set; } = string.Empty;
}

public class ConteoDenominacionRequest
{
    /// <summary>Valor de la denominación contada (ej. 20000). Debe ser una denominación válida.</summary>
    public int Denominacion { get; set; }

    [Range(0, 100_000)]
    public int Cantidad { get; set; }
}

public class CerrarTurnoRequest
{
    /// <summary>Cantidad contada por cada denominación; el total contado se calcula en el servidor.</summary>
    [Required]
    public List<ConteoDenominacionRequest> Conteo { get; set; } = new();

    /// <summary>Obligatoria cuando hay diferencia entre lo contado y lo esperado.</summary>
    [MaxLength(300)]
    public string? Nota { get; set; }
}

public record TotalesCajaResponse(
    decimal VentasEfectivo,
    decimal AbonosFiadoEfectivo,
    decimal PagosProveedorEfectivo,
    decimal Transferencias,
    decimal Tarjeta,
    decimal PagosNominaEfectivo);

public record ConteoDenominacionResponse(int Denominacion, int Cantidad, decimal Valor);

/// <summary>
/// Un turno de caja. Con el turno abierto y el conteo a ciegas activo, quien consulta (si no es
/// Admin) no recibe los totales ni el esperado: EsperadoVisible es false y esos campos van null.
/// </summary>
public record TurnoCajaResponse(
    int Id,
    int EmpleadoId,
    string EmpleadoNombre,
    DateTime FechaApertura,
    decimal BaseInicial,
    string Estado,
    bool ConteoCiego,
    bool EsperadoVisible,
    TotalesCajaResponse? Totales,
    decimal? EfectivoEsperado,
    DateTime? FechaCierre,
    string? CerradoPorNombre,
    decimal? EfectivoContado,
    decimal? Diferencia,
    string? NotaCierre,
    List<ConteoDenominacionResponse>? Conteo);

public record TurnoCajaResumenResponse(
    int Id,
    int EmpleadoId,
    string EmpleadoNombre,
    DateTime FechaApertura,
    DateTime? FechaCierre,
    string Estado,
    decimal BaseInicial,
    decimal? EfectivoEsperado,
    decimal? EfectivoContado,
    decimal? Diferencia);
