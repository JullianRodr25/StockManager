namespace StockManager.Infrastructure.Notificaciones;

/// <summary>
/// Texto de los mensajes de WhatsApp enviados por el sistema. Centralizados acá para que
/// cambiar la redacción no implique tocar el despachador ni los servicios de aplicación.
/// </summary>
public static class PlantillasMensajesWhatsApp
{
    public static string PedidoParaCliente(string nombreCliente, int pedidoId, string nuevoEstado) => nuevoEstado switch
    {
        "Pendiente" => $"¡Hola {nombreCliente}! Recibimos tu pedido #{pedidoId} y ya lo estamos revisando. Te avisaremos apenas lo confirmemos.",
        "Confirmado" => $"Tu pedido #{pedidoId} fue confirmado y pronto empezaremos a prepararlo. ¡Gracias por tu compra!",
        "EnPreparacion" => $"Estamos alistando tu pedido #{pedidoId} en bodega.",
        "EnCamino" => $"¡Tu pedido #{pedidoId} va en camino! Pronto llegará a la dirección que nos diste.",
        "Entregado" => $"Tu pedido #{pedidoId} fue entregado. ¡Gracias por comprar con nosotros! En un momento te enviamos la factura.",
        "Cancelado" => $"Tu pedido #{pedidoId} fue cancelado. Si tienes dudas, comunícate con nosotros.",
        _ => $"Tu pedido #{pedidoId} cambió de estado a {nuevoEstado}."
    };

    public static string PedidoNuevoParaAdmin(int pedidoId, string nombreCliente, decimal total) =>
        $"📦 Nuevo pedido #{pedidoId} de {nombreCliente} por ${total:N0}. Ingresa al panel para confirmarlo.";

    public static string FacturaParaCliente(string nombreCliente, string numeroFactura) =>
        $"¡Hola {nombreCliente}! Te adjuntamos la factura {numeroFactura} de tu compra. Gracias por confiar en nosotros.";
}
