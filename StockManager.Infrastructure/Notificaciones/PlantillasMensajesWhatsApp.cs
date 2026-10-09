namespace StockManager.Infrastructure.Notificaciones;

/// <summary>
/// Construye el diccionario de variables ("{{1}}", "{{2}}", ...) que llena cada plantilla de
/// WhatsApp aprobada por Meta. El texto estático de cada plantilla (el que Meta revisó y
/// aprobó) vive en Twilio, no acá — esta clase solo arma los valores dinámicos, en el mismo
/// orden en que la plantilla los espera. El texto exacto de cada plantilla, para darle
/// seguimiento o volver a enviarla a aprobación, está documentado en PLANTILLAS-WHATSAPP.md
/// en la raíz del repositorio.
///
/// Los valores nunca deben contener saltos de línea: Meta los rechaza (o los colapsa de forma
/// inconsistente) dentro de un placeholder de plantilla.
/// </summary>
public static class PlantillasMensajesWhatsApp
{
    /// <summary>
    /// Variables para la plantilla "pedido_actualizacion_cliente":
    /// "Hola {{1}}, tu pedido #{{2}} {{3}}."
    /// </summary>
    public static Dictionary<string, string> PedidoParaCliente(string nombreCliente, int pedidoId, string nuevoEstado)
    {
        var fragmento = nuevoEstado switch
        {
            "Pendiente" => "fue recibido y ya lo estamos revisando; te avisaremos apenas lo confirmemos",
            "Confirmado" => "fue confirmado y pronto empezaremos a prepararlo. ¡Gracias por tu compra!",
            "EnPreparacion" => "se está alistando en bodega",
            "EnCamino" => "va en camino. Pronto llegará a la dirección que nos diste",
            "Entregado" => "fue entregado. ¡Gracias por comprar con nosotros! En un momento te enviamos la factura",
            "Cancelado" => "fue cancelado. Si tienes dudas, comunícate con nosotros",
            _ => $"cambió de estado a {nuevoEstado}"
        };

        return new Dictionary<string, string>
        {
            ["1"] = nombreCliente,
            ["2"] = pedidoId.ToString(),
            ["3"] = fragmento
        };
    }

    /// <summary>
    /// Variables para la plantilla "pedido_nuevo_admin":
    /// "📦 Nuevo pedido #{{1}} de {{2}} por ${{3}}. Ingresa al panel para confirmarlo."
    /// </summary>
    public static Dictionary<string, string> PedidoNuevoParaAdmin(int pedidoId, string nombreCliente, decimal total) =>
        new()
        {
            ["1"] = pedidoId.ToString(),
            ["2"] = nombreCliente,
            ["3"] = total.ToString("N0")
        };

    /// <summary>
    /// Variables para la plantilla "factura_cliente" (con encabezado de documento):
    /// header = URL del PDF (dinámica); body = "¡Hola {{1}}! Te adjuntamos la factura {{2}}
    /// de tu compra. Gracias por confiar en nosotros."
    ///
    /// El número de variable que le corresponde al encabezado ("headerVariableKey") lo asigna
    /// Twilio al crear la plantilla en el Content Template Builder — suele numerarse aparte
    /// del cuerpo. Verificar en la consola de Twilio cuál índice usa el encabezado antes de
    /// dar por buena esta llave; TwilioOptions.ContentSidFacturaCliente documenta el mismo punto.
    /// </summary>
    public static Dictionary<string, string> FacturaParaCliente(string nombreCliente, string numeroFactura, string urlPdf, string headerVariableKey = "1")
    {
        var variables = new Dictionary<string, string>
        {
            [headerVariableKey] = urlPdf
        };

        // Si el encabezado no ocupa la "1", el cuerpo sigue en "1"/"2"; si la ocupa, el cuerpo
        // sigue en "2"/"3". Se resuelve así para no hardcodear una numeración que Twilio puede
        // asignar distinto a como quedó documentada.
        var siguiente = headerVariableKey == "1" ? 2 : 1;
        variables[siguiente.ToString()] = nombreCliente;
        variables[(siguiente + 1).ToString()] = numeroFactura;

        return variables;
    }

    /// <summary>
    /// Variables para la plantilla "alerta_cuenta_por_pagar":
    /// "⚠️ Cuenta por pagar a {{1}} ({{2}}): saldo ${{3}} de ${{4}} total, {{5}}."
    /// </summary>
    public static Dictionary<string, string> CuentaPorPagarProximaAVencer(
        string nombreProveedor, string concepto, decimal montoTotal, decimal saldoPendiente, DateTime fechaVencimiento)
    {
        var diasRestantes = (fechaVencimiento.Date - DateTime.UtcNow.Date).Days;
        var cuando = diasRestantes switch
        {
            < 0 => $"venció hace {Math.Abs(diasRestantes)} día(s) ({fechaVencimiento:dd/MM/yyyy})",
            0 => $"vence HOY ({fechaVencimiento:dd/MM/yyyy})",
            _ => $"vence en {diasRestantes} día(s) ({fechaVencimiento:dd/MM/yyyy})"
        };

        return new Dictionary<string, string>
        {
            ["1"] = nombreProveedor,
            ["2"] = concepto,
            ["3"] = saldoPendiente.ToString("N0"),
            ["4"] = montoTotal.ToString("N0"),
            ["5"] = cuando
        };
    }

    /// <summary>
    /// Variables para la plantilla "alerta_stock_bajo_proveedor":
    /// "📉 Hola {{1}}, los siguientes productos que nos suministras están en stock bajo:
    /// {{2}}. ¿Podrías ayudarnos a coordinar una reposición?"
    /// </summary>
    public static Dictionary<string, string> StockBajoProveedor(
        string nombreProveedor, IReadOnlyList<(string Nombre, int StockActual, int StockMinimo)> productos)
    {
        // Sin saltos de línea (Meta los rechaza dentro de un placeholder): se listan separados
        // por "; " en vez de uno por línea como hacía la versión de texto libre.
        var lista = string.Join("; ", productos.Select(p => $"{p.Nombre} (quedan {p.StockActual}, mínimo {p.StockMinimo})"));

        return new Dictionary<string, string>
        {
            ["1"] = nombreProveedor,
            ["2"] = lista
        };
    }

    /// <summary>
    /// Variables para la plantilla "alerta_stock_bajo_admin":
    /// "📉 Stock bajo: {{1}}. Quedan {{2}} unidad(es) (mínimo {{3}})."
    /// </summary>
    public static Dictionary<string, string> StockBajoAdmin(string nombreProducto, int stockActual, int stockMinimo) =>
        new()
        {
            ["1"] = nombreProducto,
            ["2"] = stockActual.ToString(),
            ["3"] = stockMinimo.ToString()
        };
}
