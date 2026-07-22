# MIGRACIÓN INICIAL GENERADA - InitialCreate

## Fecha generación: 2026-07-22
## ID Migración: 20260722004507_InitialCreate
## Total líneas SQL: 457

---

## ESTRUCTURA CREADA

### TABLAS BASE (Sin dependencias)
1. **Categorias** (4 líneas)
   - Id (PK, Identity)
   - Nombre (nvarchar(100), UNIQUE)

2. **Empleados** (6 líneas)
   - Id (PK, Identity)
   - Nombre (nvarchar(150))
   - Email (nvarchar(200), UNIQUE)
   - PasswordHash (nvarchar(max))
   - Rol (nvarchar(20))
   - Activo (bit, DEFAULT 1)

3. **Clientes** (7 líneas)
   - Id (PK, Identity)
   - Nombre (nvarchar(150))
   - Email (nvarchar(200), UNIQUE)
   - PasswordHash (nvarchar(max))
   - Telefono (nvarchar(20), INDEXED)
   - Direccion (nvarchar(300))
   - Activo (bit, DEFAULT 1)

4. **NotificacionesLog** (8 líneas)
   - Id (PK, Identity)
   - Canal (nvarchar(50))
   - Destinatario (nvarchar(200))
   - ReferenciaTipo (nvarchar(50))
   - ReferenciaId (int)
   - Estado (nvarchar(50))
   - FechaEnvio (datetime2)
   - DetalleError (nvarchar(1000), nullable)

### TABLAS CON DEPENDENCIAS

5. **Productos**
   - Id (PK, Identity)
   - Nombre (nvarchar(200), INDEXED)
   - CategoriaId (FK → Categorias, NO ACTION)
   - Precio (decimal(12,2)) ← **CRÍTICO: Precisión financiera**
   - StockActual (int)
   - StockMinimo (int)
   - Activo (bit, DEFAULT 1)
   - **RowVersion (rowversion)** ← **CRÍTICO: Concurrencia optimista**
   - CHECK constraints: Precio >= 0, StockActual >= 0

6. **Pedidos**
   - Id (PK, Identity)
   - ClienteId (FK → Clientes, NO ACTION)
   - Fecha (datetime2)
   - Estado (nvarchar(50))
   - Direccion (nvarchar(300))

7. **Ventas**
   - Id (PK, Identity)
   - EmpleadoId (FK → Empleados, NO ACTION)
   - ClienteId (FK → Clientes, nullable, NO ACTION)
   - Fecha (datetime2)
   - Total (decimal(12,2))
   - EsCotizacion (bit, DEFAULT 0)

8. **BackorderRequests**
   - Id (PK, Identity)
   - ClienteId (FK → Clientes, NO ACTION)
   - ProductoId (FK → Productos, NO ACTION)
   - CantidadDeseada (int)
   - FechaSolicitud (datetime2)
   - Estado (nvarchar(50))
   - FechaNotificacion (datetime2, nullable)
   - **ÍNDICE FILTRADO**: WHERE Estado = 'Pendiente' ← **Optimización para alertas**

9. **MovimientosStock** (Auditoría)
   - Id (PK, Identity)
   - ProductoId (FK → Productos, NO ACTION)
   - Tipo (nvarchar(50))
   - Cantidad (int)
   - Fecha (datetime2)
   - ReferenciaTipo (nvarchar(50))
   - ReferenciaId (int, nullable)
   - **ÍNDICE COMPUESTO**: (ProductoId, Fecha DESC) ← **Para historial rápido**
   - CHECK: Cantidad <> 0

10. **DetallesPedido**
	- Id (PK, Identity)
	- PedidoId (FK → Pedidos, **CASCADE**)
	- ProductoId (FK → Productos, NO ACTION)
	- Cantidad (int)
	- EstadoLinea (nvarchar(50))
	- **Estado por línea** ← Disponible | PorEncargo

11. **DetallesVenta**
	- Id (PK, Identity)
	- VentaId (FK → Ventas, **CASCADE**)
	- ProductoId (FK → Productos, NO ACTION)
	- Cantidad (int)
	- PrecioUnitario (decimal(12,2))

12. **Facturas** (La más restrictiva)
	- Id (PK, Identity)
	- VentaId (FK → Ventas, nullable, NO ACTION)
	- PedidoId (FK → Pedidos, nullable, NO ACTION)
	- Numero (nvarchar(50), UNIQUE)
	- Fecha (datetime2)
	- Total (decimal(12,2))
	- **CHECK constraint**: (VentaId IS NOT NULL AND PedidoId IS NULL) 
						 OR (VentaId IS NULL AND PedidoId IS NOT NULL)
	- ← **Exactamente uno de los dos debe estar presente**

---

## ÍNDICES GENERADOS (Total: 30+)

### Por Tabla:

**Categorias**
- IX_Categorias_Nombre (UNIQUE)

**Clientes**
- IX_Clientes_Email (UNIQUE)
- IX_Clientes_Telefono

**Empleados**
- IX_Empleados_Email (UNIQUE)

**Productos**
- IX_Productos_Nombre
- IX_Productos_CategoriaId_Activo (compuesto)
- IX_Productos_StockActual

**Ventas**
- IX_Ventas_EmpleadoId
- IX_Ventas_ClienteId
- IX_Ventas_Fecha

**DetallesVenta**
- IX_DetallesVenta_VentaId
- IX_DetallesVenta_ProductoId

**Pedidos**
- IX_Pedidos_ClienteId
- IX_Pedidos_Estado
- IX_Pedidos_Fecha

**DetallesPedido**
- IX_DetallesPedido_PedidoId
- IX_DetallesPedido_ProductoId
- IX_DetallesPedido_EstadoLinea

**BackorderRequests**
- IX_BackorderRequests_ClienteId
- IX_BackorderRequests_ProductoId_Estado (FILTRADO: WHERE Estado = 'Pendiente')
- IX_BackorderRequests_Estado
- IX_BackorderRequests_FechaSolicitud

**MovimientosStock**
- IX_MovimientosStock_ProductoId_Fecha (compuesto)
- IX_MovimientosStock_Tipo
- IX_MovimientosStock_Fecha

**Facturas**
- IX_Facturas_VentaId
- IX_Facturas_PedidoId
- IX_Facturas_Numero (UNIQUE)
- IX_Facturas_Fecha

**NotificacionesLog**
- IX_NotificacionesLog_Canal
- IX_NotificacionesLog_Destinatario
- IX_NotificacionesLog_Estado
- IX_NotificacionesLog_FechaEnvio
- IX_NotificacionesLog_ReferenciaTipo_ReferenciaId (compuesto)

---

## RELACIONES Y DELETEBEHAVIOR

### Restrict (Preserve History)
✅ Producto → DetalleVenta (NO ACTION)
✅ Producto → DetallePedido (NO ACTION)
✅ Producto → MovimientosStock (NO ACTION)
✅ Producto → BackorderRequest (NO ACTION)
✅ Empleado → Venta (NO ACTION)
✅ Cliente → Venta (NO ACTION)
✅ Cliente → Pedido (NO ACTION)
✅ Venta → Factura (NO ACTION)
✅ Pedido → Factura (NO ACTION)

**Razón**: Nunca borramos productos, empleados, clientes ni ventas
en cascada — queremos preservar todo el historial para auditoría.

### Cascade (Clean Orphans)
✅ Venta → DetalleVenta (DELETE CASCADE)
✅ Pedido → DetallesPedido (DELETE CASCADE)

**Razón**: Si se borra una venta/pedido, sus líneas no tienen sentido.

---

## CONSTRAINTS Y VALIDACIONES

### CHECK Constraints

**CK_Producto_Precio_GreaterOrEqual_Zero**
```sql
CHECK ([Precio] >= 0)
```

**CK_Producto_StockActual_GreaterOrEqual_Zero**
```sql
CHECK ([StockActual] >= 0)
```

**CK_MovimientoStock_Cantidad_NotZero**
```sql
CHECK ([Cantidad] <> 0)
```

**CK_Factura_ExactlyOneReference** (CHECK constraint custom)
```sql
CHECK (
  CASE WHEN VentaId IS NOT NULL AND PedidoId IS NULL THEN 1
	   WHEN VentaId IS NULL AND PedidoId IS NOT NULL THEN 1
	   ELSE 0
  END = 1
)
```

---

## CARACTERÍSTICAS DE CONCURRENCIA

✅ **RowVersion en Productos**
- Tipo: rowversion (SQL Server timestamp)
- Función: Token de concurrencia optimista
- Uso: Detectar conflictos cuando dos operaciones modifican el mismo producto simultáneamente (venta mostrador + pedido PWA)
- EF Core maneja automáticamente la comparación

---

## LISTA DE VERIFICACIÓN PARA APLICAR

⚠️ **IMPORTANTE**: No hemos ejecutado `dotnet ef database update` todavía.

Para aplicar la migración:

1. Asegúrate que SQL Server LocalDB esté corriendo
2. Ejecuta: `dotnet ef database update --project StockManager.Infrastructure --startup-project StockManager.Api`
3. Verifica en SQL Server que se creó la BD `StockManagerDb`
4. Comprueba que todas las tablas existen en Explorador de Objetos

---

## PRÓXIMOS PASOS

1. ✅ Configurar appsettings.json (connection string)
2. ✅ Registrar DbContext en Program.cs
3. ✅ Generar migración InitialCreate
4. ⏳ **EJECUTAR** `dotnet ef database update`
5. Crear repositorios (Repository Pattern)
6. Crear casos de uso en Application
7. Crear Controllers en Api
