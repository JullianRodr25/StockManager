# Plantillas de WhatsApp (Twilio Content Template Builder)

Todos los mensajes que el sistema manda por WhatsApp son iniciados por el negocio (avisos de
pedido, alertas al admin, avisos a proveedores, recordatorios de cuentas por pagar, envío de
factura) — ninguno es una respuesta dentro de una conversación que el destinatario abrió. Por
eso Meta exige que cada uno use una **plantilla pre-aprobada**, no texto libre: fuera de la
ventana de 24h de una conversación, un mensaje de texto libre (`Body`) es rechazado.

Este documento tiene el texto exacto de las 6 plantillas que el código espera (una por
"familia" de mensaje). Van en la consola de Twilio: **Messaging → Content Template Builder →
Create new**. Después de que Meta apruebe cada una (usualmente unas horas, a veces 1-2 días
hábiles), copiá su `ContentSid` (empieza con `HX...`) a `appsettings.json`, sección
`WhatsApp:Twilio`, en el campo correspondiente.

Mientras un `ContentSid` quede vacío, ese tipo de mensaje simplemente no se envía (queda
registrado en `NotificacionesLog` como fallido, con el motivo "Plantilla no configurada") — no
hay fallback a texto libre, para no arriesgar que Meta rechace el mensaje o penalice el número.

## 1. `pedido_actualizacion_cliente`

- **Categoría sugerida:** Utility (es una notificación de estado de un pedido que el cliente ya hizo, no marketing).
- **Idioma:** Español.
- **Cuerpo:**
  ```
  Hola {{1}}, tu pedido #{{2}} {{3}}.
  ```
- **Variables (ejemplo para que Meta las revise):**
  - `{{1}}` = María Gómez
  - `{{2}}` = 1042
  - `{{3}}` = fue confirmado y pronto empezaremos a prepararlo. ¡Gracias por tu compra!
- **Configurar en:** `WhatsApp:Twilio:ContentSidPedidoActualizacionCliente`

## 2. `pedido_nuevo_admin`

- **Categoría sugerida:** Utility.
- **Cuerpo:**
  ```
  📦 Nuevo pedido #{{1}} de {{2}} por ${{3}}. Ingresa al panel para confirmarlo.
  ```
- **Variables:**
  - `{{1}}` = 1042
  - `{{2}}` = María Gómez
  - `{{3}}` = 85.000
- **Configurar en:** `WhatsApp:Twilio:ContentSidPedidoNuevoAdmin`

## 3. `alerta_cuenta_por_pagar`

- **Categoría sugerida:** Utility.
- **Cuerpo:**
  ```
  ⚠️ Cuenta por pagar a {{1}} ({{2}}): saldo ${{3}} de ${{4}} total, {{5}}.
  ```
- **Variables:**
  - `{{1}}` = Ferrolux
  - `{{2}}` = Compra de cemento
  - `{{3}}` = 500.000
  - `{{4}}` = 800.000
  - `{{5}}` = vence en 3 día(s) (03/10/2026)
- **Configurar en:** `WhatsApp:Twilio:ContentSidAlertaCuentaPorPagar`

## 4. `alerta_stock_bajo_proveedor`

- **Categoría sugerida:** Utility.
- **Cuerpo:**
  ```
  📉 Hola {{1}}, los siguientes productos que nos suministras están en stock bajo: {{2}}. ¿Podrías ayudarnos a coordinar una reposición?
  ```
- **Variables:**
  - `{{1}}` = Ferrolux
  - `{{2}}` = Cemento gris 50kg (quedan 3, mínimo 10); Varilla 1/2" (quedan 5, mínimo 20)
- **Nota:** la lista de productos va separada por `; ` en un solo variable, sin saltos de
  línea — Meta no acepta `\n` dentro de un placeholder de plantilla.
- **Configurar en:** `WhatsApp:Twilio:ContentSidAlertaStockBajoProveedor`

## 5. `factura_cliente`

- **Categoría sugerida:** Utility.
- **Tipo de contenido:** con **encabezado de documento** (Media/Document header), no solo texto.
- **Encabezado:** documento, con una URL dinámica (el PDF de la factura).
- **Cuerpo:**
  ```
  ¡Hola {{1}}! Te adjuntamos la factura {{2}} de tu compra. Gracias por confiar en nosotros.
  ```
- **Variables:**
  - Encabezado (URL del documento) = dinámica, la genera el sistema en cada envío.
  - `{{1}}` = María Gómez
  - `{{2}}` = FAC-000123

  > ⚠️ **Importante:** al crear esta plantilla en el Content Template Builder, Twilio asigna
  > un número de variable al encabezado (normalmente `{{1}}`, con el cuerpo empezando en
  > `{{2}}`). El código (`PlantillasMensajesWhatsApp.FacturaParaCliente`) asume esa numeración
  > por defecto, pero **verificá en la consola cuál le quedó asignado** antes de dar esto por
  > terminado — si Twilio lo numeró distinto, hay que ajustar el parámetro
  > `headerVariableKey` en esa función.
- **Configurar en:** `WhatsApp:Twilio:ContentSidFacturaCliente`

## 6. `alerta_stock_bajo_admin`

- **Categoría sugerida:** Utility.
- **Idioma:** Español.
- **Cuerpo:**
  ```
  📉 Stock bajo: {{1}}. Quedan {{2}} unidad(es) (mínimo {{3}}).
  ```
- **Variables (ejemplo para que Meta las revise):**
  - `{{1}}` = Cemento gris 50kg
  - `{{2}}` = 3
  - `{{3}}` = 10
- **Destino:** el teléfono de notificaciones de administración (Configuración).
- **Se envía:** una sola vez por episodio de stock bajo (hasta que el producto se repone por encima del mínimo).
- **Configurar en:** `WhatsApp:Twilio:ContentSidAlertaStockBajoAdmin`

## Notas generales

- Las 6 son plantillas de tipo **Utility** (no Marketing), porque todas son notificaciones
  operativas ligadas a una acción que el cliente/proveedor ya inició (un pedido, una compra,
  una relación comercial existente) — eso normalmente agiliza la aprobación de Meta frente a
  Marketing.
- Meta puede pedir ajustes de redacción antes de aprobar una plantilla (por ejemplo, si algo
  suena demasiado genérico o promocional). Si eso pasa, el texto final aprobado puede no ser
  exactamente el de este documento — cuando eso ocurra, actualizá también los comentarios en
  `PlantillasMensajesWhatsApp.cs` para que sigan reflejando el texto real aprobado.
- Mientras `WhatsApp:Habilitado` sea `false` (como está hoy), nada de esto se envía —
  configurar los `ContentSid` no tiene efecto hasta que también se active ese interruptor.
