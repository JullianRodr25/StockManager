using StockManager.Application.DTOs;

namespace StockManager.Application.Services;

public interface IClienteService
{
    /// <summary>
    /// Busca clientes por nombre o número de identificación. activo=null trae todos
    /// (activos e inactivos, para el panel de administración); true/false filtra por estado
    /// (ej. true para elegir un cliente al que abrirle una venta/cuenta nueva, donde uno
    /// inactivo no debería ser seleccionable).
    /// </summary>
    Task<List<ClienteResponse>> BuscarClientesAsync(string? busqueda, bool? activo = null);

    Task<ClienteResponse?> ObtenerClientePorIdAsync(int id);

    /// <summary>
    /// Crea un cliente directamente desde el panel (Admin/Empleado). Solo los clientes
    /// registrados (ya sea así o autoregistrados en la PWA) pueden ser parte de una
    /// Cuenta Abierta (fiado).
    /// </summary>
    Task<ClienteCreadoResponse> CrearClienteAsync(CrearClienteRequest request);

    Task<ClienteResponse> ActualizarClienteAsync(int id, ActualizarClienteRequest request);

    /// <summary>
    /// Actualiza (o limpia, con campos null/vacíos) los datos fiscales que se usan para
    /// solicitar factura electrónica en una venta. Separado de ActualizarClienteAsync a
    /// propósito: son datos de un propósito distinto (facturación vs. contacto) y no siempre
    /// los edita la misma persona en el mismo momento.
    /// </summary>
    Task<ClienteResponse> ActualizarDatosFacturacionElectronicaAsync(int id, ActualizarDatosFacturacionRequest request);

    /// <summary>
    /// Desactiva un cliente. Lanza ClienteConPedidosActivosException si el cliente todavía
    /// tiene algún Pedido en estado Pendiente, Confirmado, EnPreparacion o EnCamino.
    /// </summary>
    Task<ClienteResponse> DesactivarClienteAsync(int id);

    Task<ClienteResponse> ActivarClienteAsync(int id);
}
