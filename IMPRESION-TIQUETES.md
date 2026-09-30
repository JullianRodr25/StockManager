# Impresión de tiquetes y apertura del cajón (QZ Tray)

Un navegador no puede hablarle directamente al puerto/USB de una impresora térmica: solo sabe
mandar trabajos de impresión "normales" (los que abren el diálogo de imprimir de Windows).
Para mandar los comandos crudos ESC/POS que hacen que la impresora corte el papel con el
formato de un tiquete angosto y, sobre todo, que **abra el cajón de dinero**, se necesita un
programa intermediario corriendo en el mismo computador: **QZ Tray**.

QZ Tray es gratuito, de código abierto, y es el estándar de facto para esto — lo usan
prácticamente todos los sistemas de punto de venta basados en web.

## 1. Instalar QZ Tray (una sola vez, en el computador del mostrador)

1. Descargar el instalador desde https://qz.io/download/ (versión para Windows).
2. Instalarlo como cualquier programa. Queda corriendo en segundo plano (icono en la bandeja
   del sistema) y arranca solo con Windows.
3. Conectar la impresora térmica al computador por USB (como ya la tiene Julian) e instalar su
   driver normal de Windows, si no está instalado. QZ Tray usa las impresoras que Windows ya
   reconoce — no necesita configuración aparte de driver.
4. Verificar que el cajón de dinero esté conectado al puerto de la impresora (el mismo cable
   RJ11/RJ12 que ya trae, como en el tiquete de ejemplo que mandó Julian).

## 2. Averiguar el nombre exacto de la impresora

En Windows: **Configuración → Bluetooth y dispositivos → Impresoras y escáneres**, y copiar el
nombre tal cual aparece ahí (ej. `POS-80` o `EPSON TM-T20III Receipt`). Esta app le manda los
trabajos de impresión a QZ Tray por ese nombre exacto, así que tiene que copiarse sin errores.

## 3. Configurar el nombre de la impresora en el sistema

En StockManager, ir a **Configuración → Impresora de tiquetes** (solo un administrador puede
verla y cambiarla) y pegar ahí el nombre copiado en el paso anterior. Mientras este campo esté
vacío, los botones de imprimir tiquete físico y abrir caja simplemente no aparecen — el resto
del sistema (incluida la factura en pantalla que ya existía) sigue funcionando igual.

## 4. Primer uso: aprobar la conexión

La primera vez que el navegador intenta mandar algo a QZ Tray, este muestra una ventana
preguntando si confía en el sitio. Hay que darle **Permitir** (y se puede marcar "recordar esta
decisión" para que no vuelva a preguntar). Esto es porque esta app todavía no tiene un
certificado firmado de QZ Tray — funciona igual, solo pide esa confirmación una vez por
computador.

## Qué hace el sistema con esto

- **Botón "Imprimir tiquete y abrir caja"** (en el detalle de cualquier factura, ya sea recién
  generada, desde el historial de ventas, o al cerrar una cuenta fiada): imprime un tiquete
  angosto (58/80mm) con los productos, el total, y — si el pago fue en efectivo — el monto
  recibido y el cambio. El cajón se abre automáticamente como parte del mismo trabajo de
  impresión, igual que en el sistema anterior.
- **Botón "Abrir caja"** (en la pantalla de Ventas): abre el cajón sin imprimir nada, para
  cuando el cajero necesita sacar o guardar efectivo fuera de una venta puntual.
- **"Imprimir factura"** (el botón que ya existía) sigue funcionando exactamente igual que
  antes — abre el diálogo de impresión normal del navegador, sin pasar por QZ Tray. Los dos
  botones son independientes: se puede usar uno, el otro, o ambos.

Si QZ Tray no está corriendo (computador recién prendido, programa cerrado por error, etc.), el
sistema muestra un aviso claro ("No se pudo conectar con QZ Tray...") sin afectar el resto de
la venta, que ya quedó registrada de todas formas.
