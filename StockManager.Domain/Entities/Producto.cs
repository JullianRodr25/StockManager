namespace StockManager.Domain.Entities;

using StockManager.Domain.Exceptions;

/// <summary>
/// Entidad Producto del dominio.
/// Representa un artículo del inventario con control de stock.
/// 
/// Invariantes de negocio:
/// - StockActual nunca puede ser negativo
/// - StockMinimo debe ser >= 0
/// - StockActual no se modifica directamente; solo vía métodos Vender/Reponer
/// - Precio debe ser > 0
/// - RowVersion se usa para concurrencia optimista (crítico en operaciones simultáneas de venta)
/// - CodigoBarras (opcional): EAN de fábrica o código generado internamente
/// - EsCodigoGenerado: indica si el código fue generado por el sistema (true) o vino externo (false)
/// - FechaImpresionEtiqueta (opcional): cuándo se confirmó que la etiqueta física se imprimió
/// </summary>
public class Producto
{
    /// <summary>
    /// Máximo de fotos permitidas en la galería de un producto (ver ProductoFoto). Es una
    /// constante de dominio, no de configuración: un límite bajo y fijo evita que el costo de
    /// almacenamiento en Blob Storage crezca sin control, y es más que suficiente para un
    /// catálogo de ferretería (Homecenter, la referencia, muestra 4-5 fotos por producto).
    /// </summary>
    public const int MaxFotos = 6;

    public int Id { get; private set; }
    public string Nombre { get; private set; } = null!;
    public int CategoriaId { get; private set; }
    public decimal Precio { get; private set; }
    public int StockActual { get; private set; }
    public int StockMinimo { get; private set; }

    /// <summary>
    /// True si este producto causa IVA. Cuando es false, TarifaIva siempre vale 0 — se
    /// mantiene así por invariante (ver Crear/ActualizarInformacion) para que nunca quede un
    /// estado ambiguo de "tarifa en 0 pero no se sabe si fue a propósito o quedó sin llenar".
    /// </summary>
    public bool AplicaIva { get; private set; }
    public decimal TarifaIva { get; private set; }

    /// <summary>
    /// Costo de adquisición del producto (lo que cuesta comprarlo/producirlo), para calcular
    /// métricas de rentabilidad (margen = Precio - Costo). Es puramente informativo para el
    /// negocio: nunca se expone al cliente ni participa en el cálculo de IVA o del total de
    /// una venta — eso depende solo de Precio y TarifaIva.
    /// </summary>
    public decimal Costo { get; private set; }

    public bool Activo { get; private set; }

    /// <summary>
    /// Promedio de calificaciones (1-5) de las reseñas del producto, denormalizado para no
    /// recalcularlo en cada carga del catálogo. Null mientras el producto no tiene ninguna
    /// reseña (distinto de 0: "sin calificar" no es lo mismo que "calificado con 0"). Se
    /// recalcula en ResenaService a partir de ResenasProducto, nunca se asigna a mano aquí.
    /// </summary>
    public decimal? CalificacionPromedio { get; private set; }

    /// <summary>
    /// Cantidad total de reseñas del producto, denormalizado junto con CalificacionPromedio.
    /// </summary>
    public int TotalResenas { get; private set; }

    public string? CodigoBarras { get; private set; }
    public bool EsCodigoGenerado { get; private set; }
    public DateTime? FechaImpresionEtiqueta { get; private set; }

    /// <summary>
    /// Proveedor opcional al que se le compra este producto. Un proveedor puede tener
    /// asignados muchos productos (relación uno-a-muchos); se usa para saber a quién
    /// avisarle por WhatsApp cuando el producto entra en stock bajo.
    /// </summary>
    public int? ProveedorId { get; private set; }

    /// <summary>
    /// True mientras ya se generó una NotificacionInterna de tipo "StockBajo" para el
    /// episodio actual de stock bajo (StockActual &lt;= StockMinimo) y el producto no se ha
    /// repuesto todavía. Evita que el chequeo periódico regenere la misma notificación una y
    /// otra vez; se limpia automáticamente cuando el stock vuelve a subir por encima del
    /// mínimo, para que una caída futura sí dispare un aviso nuevo.
    /// </summary>
    public bool NotificacionStockBajoActiva { get; private set; }

    // Concurrencia optimista — EF Core maneja automáticamente este campo
    public byte[]? RowVersion { get; set; }

    // Constructor privado para EF Core
    private Producto() { }

    /// <summary>
    /// Factory method para crear un nuevo Producto con validaciones completas.
    /// </summary>
    public static Producto Crear(
        string nombre,
        int categoriaId,
        decimal precio,
        int stockActual,
        int stockMinimo,
        bool aplicaIva,
        decimal tarifaIva,
        decimal costo = 0,
        string? codigoBarras = null,
        int? proveedorId = null)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre del producto no puede estar vacío.", nameof(nombre));

        if (nombre.Length > 200)
            throw new ArgumentException("El nombre del producto no puede exceder 200 caracteres.", nameof(nombre));

        if (categoriaId <= 0)
            throw new ArgumentException("CategoriaId debe ser mayor a 0.", nameof(categoriaId));

        if (precio <= 0)
            throw new ArgumentException("El precio debe ser mayor a 0.", nameof(precio));

        if (costo < 0)
            throw new ArgumentException("El costo no puede ser negativo.", nameof(costo));

        if (stockActual < 0)
            throw new ArgumentException("El stock actual no puede ser negativo.", nameof(stockActual));

        if (stockMinimo < 0)
            throw new ArgumentException("El stock mínimo no puede ser negativo.", nameof(stockMinimo));

        if (tarifaIva < 0 || tarifaIva > 100)
            throw new ArgumentException("La tarifa de IVA debe estar entre 0 y 100.", nameof(tarifaIva));

        if (!string.IsNullOrWhiteSpace(codigoBarras) && codigoBarras.Length > 50)
            throw new ArgumentException("El código de barras no puede exceder 50 caracteres.", nameof(codigoBarras));

        if (proveedorId.HasValue && proveedorId.Value <= 0)
            throw new ArgumentException("ProveedorId debe ser mayor a 0.", nameof(proveedorId));

        return new Producto
        {
            Nombre = nombre.Trim(),
            CategoriaId = categoriaId,
            Precio = precio,
            Costo = costo,
            StockActual = stockActual,
            StockMinimo = stockMinimo,
            AplicaIva = aplicaIva,
            // Invariante: un producto que no aplica IVA siempre guarda tarifa 0, para que
            // nunca quede ambigüedad sobre si el 0% fue elegido a propósito o es un resabio
            // de una tarifa que ya no aplica (ver AplicaIva).
            TarifaIva = aplicaIva ? tarifaIva : 0,
            Activo = true,
            CodigoBarras = string.IsNullOrWhiteSpace(codigoBarras) ? null : codigoBarras.Trim(),
            EsCodigoGenerado = false,
            FechaImpresionEtiqueta = null,
            ProveedorId = proveedorId
        };
    }

    /// <summary>
    /// Decrementa el stock al vender un producto.
    /// Lanza excepción si la operación violaría la invariante (stock negativo).
    /// </summary>
    /// <param name="cantidad">Cantidad a vender (debe ser > 0)</param>
    /// <returns>El nuevo stock tras la venta</returns>
    /// <exception cref="ProductoInactivoException">Si el producto está inactivo</exception>
    /// <exception cref="StockInsuficienteException">Si el stock resultante sería negativo</exception>
    public int Vender(int cantidad)
    {
        if (!Activo)
            throw new ProductoInactivoException(Id, Nombre);

        if (cantidad <= 0)
            throw new ArgumentException("La cantidad a vender debe ser mayor a 0.", nameof(cantidad));

        if (StockActual - cantidad < 0)
            throw new StockInsuficienteException(StockActual, cantidad);

        StockActual -= cantidad;
        return StockActual;
    }

    /// <summary>
    /// Incrementa el stock al reponer inventario.
    /// </summary>
    /// <param name="cantidad">Cantidad a reponer (debe ser > 0)</param>
    /// <returns>El nuevo stock tras la reposición</returns>
    /// <exception cref="ArgumentException">Si la cantidad no es válida</exception>
    public int Reponer(int cantidad)
    {
        if (cantidad <= 0)
            throw new ArgumentException("La cantidad a reponer debe ser mayor a 0.", nameof(cantidad));

        StockActual += cantidad;
        return StockActual;
    }

    /// <summary>
    /// Ajuste manual del stock (ej. mercancía que llega, corrección de un conteo físico), desde
    /// el panel de Inventario — no pasa por Vender()/Reponer() porque no está atado a una venta
    /// ni a un pedido, y a propósito no exige Activo = true (corregir el conteo de un producto
    /// inactivo sigue siendo válido). delta puede ser positivo o negativo; nunca puede dejar el
    /// stock en negativo.
    /// </summary>
    /// <param name="delta">Cuánto sumar (positivo) o restar (negativo) al stock actual.</param>
    /// <returns>El nuevo stock tras el ajuste</returns>
    public int AjustarStock(int delta)
    {
        if (delta == 0)
            throw new ArgumentException("El ajuste no puede ser 0.", nameof(delta));

        var nuevoStock = StockActual + delta;
        if (nuevoStock < 0)
            throw new StockInsuficienteException(StockActual, -delta);

        StockActual = nuevoStock;
        return StockActual;
    }

    /// <summary>
    /// Desactiva el producto.
    /// </summary>
    public void Desactivar()
    {
        Activo = false;
    }

    /// <summary>
    /// Reactiva el producto.
    /// </summary>
    public void Activar()
    {
        Activo = true;
    }

    /// <summary>
    /// Actualiza el precio del producto.
    /// </summary>
    public void ActualizarPrecio(decimal nuevoPrecio)
    {
        if (nuevoPrecio <= 0)
            throw new ArgumentException("El precio debe ser mayor a 0.", nameof(nuevoPrecio));

        Precio = nuevoPrecio;
    }

    /// <summary>
    /// Actualiza el stock mínimo para alertas.
    /// </summary>
    public void ActualizarStockMinimo(int nuevoStockMinimo)
    {
        if (nuevoStockMinimo < 0)
            throw new ArgumentException("El stock mínimo no puede ser negativo.", nameof(nuevoStockMinimo));

        StockMinimo = nuevoStockMinimo;
    }

    /// <summary>
    /// Actualiza la información general del producto (nombre, categoría, precio, costo, stock
    /// mínimo, IVA y código de barras). NO modifica StockActual; el stock solo cambia vía
    /// Vender()/Reponer().
    /// </summary>
    public void ActualizarInformacion(
        string nombre,
        int categoriaId,
        decimal precio,
        decimal costo,
        int stockMinimo,
        bool aplicaIva,
        decimal tarifaIva,
        string? codigoBarras,
        int? proveedorId = null)
    {
        if (string.IsNullOrWhiteSpace(nombre))
            throw new ArgumentException("El nombre no puede estar vacío.");
        if (precio < 0)
            throw new ArgumentException("El precio no puede ser negativo.");
        if (costo < 0)
            throw new ArgumentException("El costo no puede ser negativo.");
        if (stockMinimo < 0)
            throw new ArgumentException("El stock mínimo no puede ser negativo.");
        if (tarifaIva < 0 || tarifaIva > 100)
            throw new ArgumentException("La tarifa de IVA debe estar entre 0 y 100.");
        if (proveedorId.HasValue && proveedorId.Value <= 0)
            throw new ArgumentException("ProveedorId debe ser mayor a 0.", nameof(proveedorId));

        Nombre = nombre.Trim();
        CategoriaId = categoriaId;
        Precio = precio;
        Costo = costo;
        StockMinimo = stockMinimo;
        AplicaIva = aplicaIva;
        TarifaIva = aplicaIva ? tarifaIva : 0;
        CodigoBarras = string.IsNullOrWhiteSpace(codigoBarras) ? CodigoBarras : codigoBarras.Trim();
        ProveedorId = proveedorId;
    }

    /// <summary>
    /// Genera un código de barras interno basado en el Id del producto.
    /// Formato: "INT-{Id:D8}" (8 dígitos con ceros a la izquierda).
    /// Solo se ejecuta si CodigoBarras es actualmente null.
    /// Marca EsCodigoGenerado = true.
    /// </summary>
    public void GenerarCodigoBarrasInterno()
    {
        if (Id <= 0)
            throw new InvalidOperationException("No se puede generar un código de barras sin un Id válido. El producto debe estar guardado en la base de datos.");

        if (CodigoBarras != null)
            return; // Ya tiene un código, no se sobrescribe

        CodigoBarras = $"INT-{Id:D8}";
        EsCodigoGenerado = true;
    }

    /// <summary>
    /// Marca que la etiqueta física con el código de barras ha sido impresa.
    /// </summary>
    public void MarcarEtiquetaImpresa()
    {
        FechaImpresionEtiqueta = DateTime.UtcNow;
    }

    /// <summary>
    /// Marca que ya se generó la notificación interna de stock bajo para el episodio actual.
    /// </summary>
    public void MarcarNotificacionStockBajoActiva()
    {
        NotificacionStockBajoActiva = true;
    }

    /// <summary>
    /// Limpia la bandera de stock bajo (el producto se repuso por encima del mínimo), para
    /// que una futura caída por debajo del mínimo dispare una notificación nueva.
    /// </summary>
    public void LimpiarNotificacionStockBajo()
    {
        NotificacionStockBajoActiva = false;
    }

    /// <summary>
    /// Reemplaza CalificacionPromedio/TotalResenas con los valores recién recalculados desde
    /// ResenasProducto (ver ResenaService.RecalcularAgregadosAsync). No se expone forma de
    /// "sumar" o ajustar a mano: siempre se recalcula completo desde la fuente de verdad
    /// (las reseñas), para que nunca quede desincronizado por un cálculo incremental con error
    /// acumulado.
    /// </summary>
    public void ActualizarCalificacion(decimal? calificacionPromedio, int totalResenas)
    {
        if (totalResenas < 0)
            throw new ArgumentException("TotalResenas no puede ser negativo.", nameof(totalResenas));

        if (totalResenas == 0 && calificacionPromedio != null)
            throw new ArgumentException(
                "No puede haber un promedio de calificación sin reseñas.", nameof(calificacionPromedio));

        if (calificacionPromedio is < 1 or > 5)
            throw new ArgumentException(
                "El promedio de calificación debe estar entre 1 y 5.", nameof(calificacionPromedio));

        CalificacionPromedio = calificacionPromedio;
        TotalResenas = totalResenas;
    }
}
