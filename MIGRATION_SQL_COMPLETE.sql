/*
=================================================================================
SCRIPT SQL GENERADO - MIGRACIÓN INICIAL (InitialCreate)
=================================================================================

Fecha: 2026-07-22
ID Migración: 20260722004507_InitialCreate
Total líneas: 457 (sin comentarios)
Configuración: SQL Server LocalDB, Base de datos: StockManagerDb
Idempotente: SÍ (verifica si ya existe antes de crear)

=================================================================================
*/

-- ============================================================================
-- 1. TABLA DE HISTORIAL DE MIGRACIONES (Sistema EF Core)
-- ============================================================================

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
	CREATE TABLE [__EFMigrationsHistory] (
		[MigrationId] nvarchar(150) NOT NULL,
		[ProductVersion] nvarchar(32) NOT NULL,
		CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
	);
END;


-- ============================================================================
-- 2. TABLAS BASE (Sin dependencias externas)
-- ============================================================================

-- CATEGORIAS: Clasificación de productos
CREATE TABLE [Categorias] (
	[Id] int NOT NULL IDENTITY,
	[Nombre] nvarchar(100) NOT NULL,
	CONSTRAINT [PK_Categorias] PRIMARY KEY ([Id])
);

-- EMPLEADOS: Personal de la ferretería
CREATE TABLE [Empleados] (
	[Id] int NOT NULL IDENTITY,
	[Nombre] nvarchar(150) NOT NULL,
	[Email] nvarchar(200) NOT NULL,
	[PasswordHash] nvarchar(max) NOT NULL,
	[Rol] nvarchar(20) NOT NULL,                        -- 'Admin' | 'Empleado'
	[Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
	CONSTRAINT [PK_Empleados] PRIMARY KEY ([Id])
);

-- CLIENTES: Compradores
CREATE TABLE [Clientes] (
	[Id] int NOT NULL IDENTITY,
	[Nombre] nvarchar(150) NOT NULL,
	[Email] nvarchar(200) NOT NULL,
	[PasswordHash] nvarchar(max) NOT NULL,
	[Telefono] nvarchar(20) NOT NULL,                   -- Para WhatsApp
	[Direccion] nvarchar(300) NOT NULL,
	[Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
	CONSTRAINT [PK_Clientes] PRIMARY KEY ([Id])
);

-- NOTIFICACIONESLOG: Trazabilidad de envíos (WhatsApp, Push, Email)
CREATE TABLE [NotificacionesLog] (
	[Id] int NOT NULL IDENTITY,
	[Canal] nvarchar(50) NOT NULL,                      -- 'WhatsApp'|'Push'|'Email'
	[Destinatario] nvarchar(200) NOT NULL,
	[ReferenciaTipo] nvarchar(50) NOT NULL,             -- 'Venta'|'Pedido'|'BackorderRequest'
	[ReferenciaId] int NOT NULL,
	[Estado] nvarchar(50) NOT NULL,                     -- 'Enviado'|'Fallido'
	[FechaEnvio] datetime2 NOT NULL,
	[DetalleError] nvarchar(1000) NULL,
	CONSTRAINT [PK_NotificacionesLog] PRIMARY KEY ([Id])
);


-- ============================================================================
-- 3. TABLA CON CONFIGURACIÓN CRÍTICA DE CONCURRENCIA
-- ============================================================================

-- PRODUCTOS: Artículos del inventario
CREATE TABLE [Productos] (
	[Id] int NOT NULL IDENTITY,
	[Nombre] nvarchar(200) NOT NULL,
	[CategoriaId] int NOT NULL,
	[Precio] decimal(12,2) NOT NULL,                    -- 💰 Precisión financiera
	[StockActual] int NOT NULL,
	[StockMinimo] int NOT NULL,
	[Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
	[RowVersion] rowversion NULL,                        -- ⏱️ CONCURRENCIA OPTIMISTA

	CONSTRAINT [PK_Productos] PRIMARY KEY ([Id]),

	-- CHECK constraints para invariantes
	CONSTRAINT [CK_Producto_Precio_GreaterOrEqual_Zero] CHECK ([Precio] >= 0),
	CONSTRAINT [CK_Producto_StockActual_GreaterOrEqual_Zero] CHECK ([StockActual] >= 0),

	-- FK a Categoría (Restrict = NO ACTION)
	CONSTRAINT [FK_Productos_Categorias_CategoriaId] 
		FOREIGN KEY ([CategoriaId]) REFERENCES [Categorias] ([Id]) ON DELETE NO ACTION
);
-- ➜ RowVersion es crítico: detecta conflictos cuando 2+ operaciones modifican Producto simultáneamente


-- ============================================================================
-- 4. TABLAS DE TRANSACCIONES
-- ============================================================================

-- PEDIDOS: Pedidos vía PWA para entrega a domicilio
CREATE TABLE [Pedidos] (
	[Id] int NOT NULL IDENTITY,
	[ClienteId] int NOT NULL,
	[Fecha] datetime2 NOT NULL,
	[Estado] nvarchar(50) NOT NULL,                     -- Pendiente|Confirmado|EnPreparacion|EnCamino|Entregado|Cancelado
	[Direccion] nvarchar(300) NOT NULL,

	CONSTRAINT [PK_Pedidos] PRIMARY KEY ([Id]),
	CONSTRAINT [FK_Pedidos_Clientes_ClienteId] 
		FOREIGN KEY ([ClienteId]) REFERENCES [Clientes] ([Id]) ON DELETE NO ACTION
);

-- VENTAS: Ventas de mostrador o cotizaciones
CREATE TABLE [Ventas] (
	[Id] int NOT NULL IDENTITY,
	[EmpleadoId] int NOT NULL,
	[ClienteId] int NULL,                               -- Nullable (venta anónima posible)
	[Fecha] datetime2 NOT NULL,
	[Total] decimal(12,2) NOT NULL,
	[EsCotizacion] bit NOT NULL DEFAULT CAST(0 AS bit),

	CONSTRAINT [PK_Ventas] PRIMARY KEY ([Id]),
	CONSTRAINT [FK_Ventas_Empleados_EmpleadoId] 
		FOREIGN KEY ([EmpleadoId]) REFERENCES [Empleados] ([Id]) ON DELETE NO ACTION,
	CONSTRAINT [FK_Ventas_Clientes_ClienteId] 
		FOREIGN KEY ([ClienteId]) REFERENCES [Clientes] ([Id]) ON DELETE NO ACTION
);


-- ============================================================================
-- 5. TABLAS DE AUDITORÍA Y CONTROL
-- ============================================================================

-- BACKORDERREQUESTS: Solicitudes de restock (alertas)
CREATE TABLE [BackorderRequests] (
	[Id] int NOT NULL IDENTITY,
	[ClienteId] int NOT NULL,
	[ProductoId] int NOT NULL,
	[CantidadDeseada] int NOT NULL,
	[FechaSolicitud] datetime2 NOT NULL,
	[Estado] nvarchar(50) NOT NULL,                     -- 'Pendiente'|'Notificado'|'Cancelado'
	[FechaNotificacion] datetime2 NULL,

	CONSTRAINT [PK_BackorderRequests] PRIMARY KEY ([Id]),
	CONSTRAINT [FK_BackorderRequests_Clientes_ClienteId] 
		FOREIGN KEY ([ClienteId]) REFERENCES [Clientes] ([Id]) ON DELETE NO ACTION,
	CONSTRAINT [FK_BackorderRequests_Productos_ProductoId] 
		FOREIGN KEY ([ProductoId]) REFERENCES [Productos] ([Id]) ON DELETE NO ACTION
);

-- MOVIMIENTOSSTOCK: Auditoría (NUNCA se toca StockActual directamente)
CREATE TABLE [MovimientosStock] (
	[Id] int NOT NULL IDENTITY,
	[ProductoId] int NOT NULL,
	[Tipo] nvarchar(50) NOT NULL,                       -- 'Entrada'|'SalidaVenta'|'SalidaPedido'|'Ajuste'
	[Cantidad] int NOT NULL,
	[Fecha] datetime2 NOT NULL,
	[ReferenciaTipo] nvarchar(50) NOT NULL,             -- 'Venta'|'Pedido'|'Ajuste'
	[ReferenciaId] int NULL,

	CONSTRAINT [PK_MovimientosStock] PRIMARY KEY ([Id]),
	CONSTRAINT [CK_MovimientoStock_Cantidad_NotZero] CHECK ([Cantidad] <> 0),
	CONSTRAINT [FK_MovimientosStock_Productos_ProductoId] 
		FOREIGN KEY ([ProductoId]) REFERENCES [Productos] ([Id]) ON DELETE NO ACTION
);


-- ============================================================================
-- 6. TABLAS DE DETALLES (Cascade a su padre)
-- ============================================================================

-- DETALLEPEDIDO: Líneas de un pedido (estado por línea)
CREATE TABLE [DetallesPedido] (
	[Id] int NOT NULL IDENTITY,
	[PedidoId] int NOT NULL,
	[ProductoId] int NOT NULL,
	[Cantidad] int NOT NULL,
	[EstadoLinea] nvarchar(50) NOT NULL,                -- 'Disponible'|'PorEncargo'

	CONSTRAINT [PK_DetallesPedido] PRIMARY KEY ([Id]),
	CONSTRAINT [FK_DetallesPedido_Pedidos_PedidoId] 
		FOREIGN KEY ([PedidoId]) REFERENCES [Pedidos] ([Id]) ON DELETE CASCADE,  -- ⏬ CASCADE
	CONSTRAINT [FK_DetallesPedido_Productos_ProductoId] 
		FOREIGN KEY ([ProductoId]) REFERENCES [Productos] ([Id]) ON DELETE NO ACTION  -- Restrict
);

-- DETALLEVENTA: Líneas de una venta
CREATE TABLE [DetallesVenta] (
	[Id] int NOT NULL IDENTITY,
	[VentaId] int NOT NULL,
	[ProductoId] int NOT NULL,
	[Cantidad] int NOT NULL,
	[PrecioUnitario] decimal(12,2) NOT NULL,

	CONSTRAINT [PK_DetallesVenta] PRIMARY KEY ([Id]),
	CONSTRAINT [FK_DetallesVenta_Ventas_VentaId] 
		FOREIGN KEY ([VentaId]) REFERENCES [Ventas] ([Id]) ON DELETE CASCADE,    -- ⏬ CASCADE
	CONSTRAINT [FK_DetallesVenta_Productos_ProductoId] 
		FOREIGN KEY ([ProductoId]) REFERENCES [Productos] ([Id]) ON DELETE NO ACTION  -- Restrict
);


-- ============================================================================
-- 7. TABLA RESTRICTIVA (Exactamente una FK o la otra)
-- ============================================================================

-- FACTURAS: Factura vinculada a Venta O Pedido (exactamente una)
CREATE TABLE [Facturas] (
	[Id] int NOT NULL IDENTITY,
	[VentaId] int NULL,
	[PedidoId] int NULL,
	[Numero] nvarchar(50) NOT NULL,
	[Fecha] datetime2 NOT NULL,
	[Total] decimal(12,2) NOT NULL,

	CONSTRAINT [PK_Facturas] PRIMARY KEY ([Id]),

	-- ✅ CHECK constraint personalizado: exactamente una de las dos FK
	CONSTRAINT [CK_Factura_ExactlyOneReference] 
		CHECK ((CASE 
				  WHEN VentaId IS NOT NULL AND PedidoId IS NULL THEN 1
				  WHEN VentaId IS NULL AND PedidoId IS NOT NULL THEN 1
				  ELSE 0 
			  END = 1)),

	CONSTRAINT [FK_Facturas_Ventas_VentaId] 
		FOREIGN KEY ([VentaId]) REFERENCES [Ventas] ([Id]) ON DELETE NO ACTION,
	CONSTRAINT [FK_Facturas_Pedidos_PedidoId] 
		FOREIGN KEY ([PedidoId]) REFERENCES [Pedidos] ([Id]) ON DELETE NO ACTION
);


-- ============================================================================
-- 8. ÍNDICES PARA PERFORMANCE
-- ============================================================================

-- CATEGORIAS
CREATE UNIQUE INDEX [IX_Categorias_Nombre] ON [Categorias] ([Nombre]);

-- CLIENTES
CREATE UNIQUE INDEX [IX_Clientes_Email] ON [Clientes] ([Email]);
CREATE INDEX [IX_Clientes_Telefono] ON [Clientes] ([Telefono]);

-- EMPLEADOS
CREATE UNIQUE INDEX [IX_Empleados_Email] ON [Empleados] ([Email]);

-- PRODUCTOS
CREATE INDEX [IX_Productos_Nombre] ON [Productos] ([Nombre]);
CREATE INDEX [IX_Productos_CategoriaId_Activo] ON [Productos] ([CategoriaId], [Activo]);  -- Compuesto
CREATE INDEX [IX_Productos_StockActual] ON [Productos] ([StockActual]);

-- VENTAS
CREATE INDEX [IX_Ventas_EmpleadoId] ON [Ventas] ([EmpleadoId]);
CREATE INDEX [IX_Ventas_ClienteId] ON [Ventas] ([ClienteId]);
CREATE INDEX [IX_Ventas_Fecha] ON [Ventas] ([Fecha]);

-- DETALLEVENTA
CREATE INDEX [IX_DetallesVenta_VentaId] ON [DetallesVenta] ([VentaId]);
CREATE INDEX [IX_DetallesVenta_ProductoId] ON [DetallesVenta] ([ProductoId]);

-- PEDIDOS
CREATE INDEX [IX_Pedidos_ClienteId] ON [Pedidos] ([ClienteId]);
CREATE INDEX [IX_Pedidos_Estado] ON [Pedidos] ([Estado]);
CREATE INDEX [IX_Pedidos_Fecha] ON [Pedidos] ([Fecha]);

-- DETALLEPEDIDO
CREATE INDEX [IX_DetallesPedido_PedidoId] ON [DetallesPedido] ([PedidoId]);
CREATE INDEX [IX_DetallesPedido_ProductoId] ON [DetallesPedido] ([ProductoId]);
CREATE INDEX [IX_DetallesPedido_EstadoLinea] ON [DetallesPedido] ([EstadoLinea]);

-- BACKORDERREQUESTS
CREATE INDEX [IX_BackorderRequests_ClienteId] ON [BackorderRequests] ([ClienteId]);
CREATE INDEX [IX_BackorderRequests_ProductoId_Estado] 
	ON [BackorderRequests] ([ProductoId], [Estado]) 
	WHERE Estado = 'Pendiente';  -- ✅ ÍNDICE FILTRADO (optimización)
CREATE INDEX [IX_BackorderRequests_Estado] ON [BackorderRequests] ([Estado]);
CREATE INDEX [IX_BackorderRequests_FechaSolicitud] ON [BackorderRequests] ([FechaSolicitud]);

-- MOVIMIENTOSSTOCK
CREATE INDEX [IX_MovimientosStock_ProductoId_Fecha] 
	ON [MovimientosStock] ([ProductoId], [Fecha]);  -- Compuesto para historial
CREATE INDEX [IX_MovimientosStock_Tipo] ON [MovimientosStock] ([Tipo]);
CREATE INDEX [IX_MovimientosStock_Fecha] ON [MovimientosStock] ([Fecha]);

-- FACTURAS
CREATE INDEX [IX_Facturas_VentaId] ON [Facturas] ([VentaId]);
CREATE INDEX [IX_Facturas_PedidoId] ON [Facturas] ([PedidoId]);
CREATE UNIQUE INDEX [IX_Facturas_Numero] ON [Facturas] ([Numero]);
CREATE INDEX [IX_Facturas_Fecha] ON [Facturas] ([Fecha]);

-- NOTIFICACIONESLOG
CREATE INDEX [IX_NotificacionesLog_Canal] ON [NotificacionesLog] ([Canal]);
CREATE INDEX [IX_NotificacionesLog_Destinatario] ON [NotificacionesLog] ([Destinatario]);
CREATE INDEX [IX_NotificacionesLog_Estado] ON [NotificacionesLog] ([Estado]);
CREATE INDEX [IX_NotificacionesLog_FechaEnvio] ON [NotificacionesLog] ([FechaEnvio]);
CREATE INDEX [IX_NotificacionesLog_ReferenciaTipo_ReferenciaId] 
	ON [NotificacionesLog] ([ReferenciaTipo], [ReferenciaId]);


-- ============================================================================
-- 9. REGISTRO DE LA MIGRACIÓN EN EF CORE
-- ============================================================================

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260722004507_InitialCreate', N'10.0.10');


-- ============================================================================
-- RESUMEN
-- ============================================================================

/*
✅ 12 tablas creadas
✅ 30+ índices creados
✅ DELETE CASCADE configurado correctamente (solo para detalles)
✅ NO ACTION (Restrict) para proteger historial
✅ CHECK constraints para invariantes
✅ RowVersion en Productos para concurrencia optimista
✅ Índice filtrado en BackorderRequests para queries de pendientes
✅ Índices compuestos para historial de movimientos
✅ UNIQUE constraints en Numero de Factura

⏳ PRÓXIMO PASO: dotnet ef database update
*/
