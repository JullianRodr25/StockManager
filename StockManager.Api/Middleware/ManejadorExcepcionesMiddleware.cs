using Microsoft.EntityFrameworkCore;
using StockManager.Domain.Exceptions;

namespace StockManager.Api.Middleware;

/// <summary>
/// Último punto de captura de la API: traduce cualquier excepción que ningún controlador haya
/// atrapado a una respuesta JSON <c>{ "message": "..." }</c> con el código HTTP que corresponde.
///
/// Por qué existe: sin esto, una excepción sin atrapar sale como un 500 sin cuerpo y el frontend
/// (que solo sabe leer <c>message</c>) muestra "Ocurrió un error inesperado" aunque el error fuera
/// uno controlado de negocio, como "este producto ya existe". Centralizarlo aquí evita repetir
/// try/catch en cada acción y garantiza que todos los errores de dominio lleguen al usuario con su
/// mensaje real.
///
/// Va DESPUÉS de UseCors a propósito: es un middleware propio (no UseExceptionHandler) porque ese
/// limpia la respuesta, incluidas las cabeceras CORS, y el navegador terminaría bloqueando el error
/// en lugar de mostrarlo.
/// </summary>
public sealed class ManejadorExcepcionesMiddleware
{
    private const string MensajeErrorInterno = "Ocurrió un error inesperado en el servidor. Intenta de nuevo en unos segundos.";

    private readonly RequestDelegate _next;
    private readonly ILogger<ManejadorExcepcionesMiddleware> _logger;

    public ManejadorExcepcionesMiddleware(RequestDelegate next, ILogger<ManejadorExcepcionesMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            var (status, mensaje) = Traducir(ex);

            // Los errores esperados de negocio no son fallos del servidor: se registran como aviso
            // para no ensuciar el log con "errores" que son validaciones normales.
            if (status >= 500)
                _logger.LogError(ex, "Excepción no controlada en {Metodo} {Ruta}", context.Request.Method, context.Request.Path);
            else
                _logger.LogWarning("{Metodo} {Ruta} rechazado ({Status}): {Mensaje}", context.Request.Method, context.Request.Path, status, mensaje);

            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new { message = mensaje });
        }
    }

    private static (int Status, string Mensaje) Traducir(Exception ex) => ex switch
    {
        // Recurso inexistente
        ProductoNoEncontradoException
            or VentaNoEncontradaException
            or PedidoNoEncontradoException
            or ProveedorNoEncontradoException
            or CuentaPorPagarNoEncontradaException
            or ClienteNoEncontradoException
            or ProductoFotoNoEncontradaException
            or NotificacionInternaNoEncontradaException
            or ResenaNoEncontradaException => (StatusCodes.Status404NotFound, ex.Message),

        // Choque con el estado actual: duplicados, estados inválidos o edición concurrente
        ConcurrencyException
            or UsuarioDuplicadoPorIdentificacionException
            or UsuarioDuplicadoPorEmailException
            or CategoriaDuplicadaException
            or ProductoDuplicadoException
            or ResenaDuplicadaException
            or CuentaFiadoAbiertaException
            or CuentaConAbonosException
            or VentaEstadoInvalidoException
            or PedidoEstadoInvalidoException
            or CuentaPorPagarEstadoInvalidoException
            or ClienteConPedidosActivosException => (StatusCodes.Status409Conflict, ex.Message),

        // Cualquier otra regla de negocio incumplida (stock insuficiente, operación inválida, etc.)
        DomainException => (StatusCodes.Status400BadRequest, ex.Message),

        ArgumentException argumento => (StatusCodes.Status400BadRequest, MensajeSinNombreDeParametro(argumento)),

        // Respaldo por si una violación del índice único se escapó de las validaciones previas.
        DbUpdateException { InnerException: not null } actualizacion
            when actualizacion.InnerException!.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
            => (StatusCodes.Status409Conflict, "Ya existe un registro con esos mismos datos."),

        // Nunca se expone el detalle técnico al cliente: queda solo en el log del servidor.
        _ => (StatusCodes.Status500InternalServerError, MensajeErrorInterno),
    };

    // ArgumentException.Message agrega " (Parameter 'x')" al final, que no tiene sentido para el usuario.
    private static string MensajeSinNombreDeParametro(ArgumentException ex)
    {
        var indice = ex.Message.IndexOf(" (Parameter '", StringComparison.Ordinal);
        return indice > 0 ? ex.Message[..indice] : ex.Message;
    }
}
