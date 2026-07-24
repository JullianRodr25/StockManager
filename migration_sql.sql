IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Categorias] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(100) NOT NULL,
    CONSTRAINT [PK_Categorias] PRIMARY KEY ([Id])
);

CREATE TABLE [Clientes] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(150) NOT NULL,
    [Email] nvarchar(200) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [Telefono] nvarchar(20) NOT NULL,
    [Direccion] nvarchar(300) NOT NULL,
    [Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_Clientes] PRIMARY KEY ([Id])
);

CREATE TABLE [Empleados] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(150) NOT NULL,
    [Email] nvarchar(200) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [Rol] nvarchar(20) NOT NULL,
    [Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
    CONSTRAINT [PK_Empleados] PRIMARY KEY ([Id])
);

CREATE TABLE [NotificacionesLog] (
    [Id] int NOT NULL IDENTITY,
    [Canal] nvarchar(50) NOT NULL,
    [Destinatario] nvarchar(200) NOT NULL,
    [ReferenciaTipo] nvarchar(50) NOT NULL,
    [ReferenciaId] int NOT NULL,
    [Estado] nvarchar(50) NOT NULL,
    [FechaEnvio] datetime2 NOT NULL,
    [DetalleError] nvarchar(1000) NULL,
    CONSTRAINT [PK_NotificacionesLog] PRIMARY KEY ([Id])
);

CREATE TABLE [Productos] (
    [Id] int NOT NULL IDENTITY,
    [Nombre] nvarchar(200) NOT NULL,
    [CategoriaId] int NOT NULL,
    [Precio] decimal(12,2) NOT NULL,
    [StockActual] int NOT NULL,
    [StockMinimo] int NOT NULL,
    [Activo] bit NOT NULL DEFAULT CAST(1 AS bit),
    [RowVersion] rowversion NULL,
    CONSTRAINT [PK_Productos] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Producto_Precio_GreaterOrEqual_Zero] CHECK ([Precio] >= 0),
    CONSTRAINT [CK_Producto_StockActual_GreaterOrEqual_Zero] CHECK ([StockActual] >= 0),
    CONSTRAINT [FK_Productos_Categorias_CategoriaId] FOREIGN KEY ([CategoriaId]) REFERENCES [Categorias] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Pedidos] (
    [Id] int NOT NULL IDENTITY,
    [ClienteId] int NOT NULL,
    [Fecha] datetime2 NOT NULL,
    [Estado] nvarchar(50) NOT NULL,
    [Direccion] nvarchar(300) NOT NULL,
    CONSTRAINT [PK_Pedidos] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Pedidos_Clientes_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clientes] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [Ventas] (
    [Id] int NOT NULL IDENTITY,
    [EmpleadoId] int NOT NULL,
    [ClienteId] int NULL,
    [Fecha] datetime2 NOT NULL,
    [Total] decimal(12,2) NOT NULL,
    [EsCotizacion] bit NOT NULL DEFAULT CAST(0 AS bit),
    CONSTRAINT [PK_Ventas] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_Ventas_Clientes_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clientes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Ventas_Empleados_EmpleadoId] FOREIGN KEY ([EmpleadoId]) REFERENCES [Empleados] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [BackorderRequests] (
    [Id] int NOT NULL IDENTITY,
    [ClienteId] int NOT NULL,
    [ProductoId] int NOT NULL,
    [CantidadDeseada] int NOT NULL,
    [FechaSolicitud] datetime2 NOT NULL,
    [Estado] nvarchar(50) NOT NULL,
    [FechaNotificacion] datetime2 NULL,
    CONSTRAINT [PK_BackorderRequests] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_BackorderRequests_Clientes_ClienteId] FOREIGN KEY ([ClienteId]) REFERENCES [Clientes] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_BackorderRequests_Productos_ProductoId] FOREIGN KEY ([ProductoId]) REFERENCES [Productos] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [MovimientosStock] (
    [Id] int NOT NULL IDENTITY,
    [ProductoId] int NOT NULL,
    [Tipo] nvarchar(50) NOT NULL,
    [Cantidad] int NOT NULL,
    [Fecha] datetime2 NOT NULL,
    [ReferenciaTipo] nvarchar(50) NOT NULL,
    [ReferenciaId] int NULL,
    CONSTRAINT [PK_MovimientosStock] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_MovimientoStock_Cantidad_NotZero] CHECK ([Cantidad] <> 0),
    CONSTRAINT [FK_MovimientosStock_Productos_ProductoId] FOREIGN KEY ([ProductoId]) REFERENCES [Productos] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [DetallesPedido] (
    [Id] int NOT NULL IDENTITY,
    [PedidoId] int NOT NULL,
    [ProductoId] int NOT NULL,
    [Cantidad] int NOT NULL,
    [EstadoLinea] nvarchar(50) NOT NULL,
    CONSTRAINT [PK_DetallesPedido] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DetallesPedido_Pedidos_PedidoId] FOREIGN KEY ([PedidoId]) REFERENCES [Pedidos] ([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_DetallesPedido_Productos_ProductoId] FOREIGN KEY ([ProductoId]) REFERENCES [Productos] ([Id]) ON DELETE NO ACTION
);

CREATE TABLE [DetallesVenta] (
    [Id] int NOT NULL IDENTITY,
    [VentaId] int NOT NULL,
    [ProductoId] int NOT NULL,
    [Cantidad] int NOT NULL,
    [PrecioUnitario] decimal(12,2) NOT NULL,
    CONSTRAINT [PK_DetallesVenta] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_DetallesVenta_Productos_ProductoId] FOREIGN KEY ([ProductoId]) REFERENCES [Productos] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_DetallesVenta_Ventas_VentaId] FOREIGN KEY ([VentaId]) REFERENCES [Ventas] ([Id]) ON DELETE CASCADE
);

CREATE TABLE [Facturas] (
    [Id] int NOT NULL IDENTITY,
    [VentaId] int NULL,
    [PedidoId] int NULL,
    [Numero] nvarchar(50) NOT NULL,
    [Fecha] datetime2 NOT NULL,
    [Total] decimal(12,2) NOT NULL,
    CONSTRAINT [PK_Facturas] PRIMARY KEY ([Id]),
    CONSTRAINT [CK_Factura_ExactlyOneReference] CHECK ((CASE WHEN VentaId IS NOT NULL AND PedidoId IS NULL THEN 1 WHEN VentaId IS NULL AND PedidoId IS NOT NULL THEN 1 ELSE 0 END = 1)),
    CONSTRAINT [FK_Facturas_Pedidos_PedidoId] FOREIGN KEY ([PedidoId]) REFERENCES [Pedidos] ([Id]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Facturas_Ventas_VentaId] FOREIGN KEY ([VentaId]) REFERENCES [Ventas] ([Id]) ON DELETE NO ACTION
);

CREATE INDEX [IX_BackorderRequests_ClienteId] ON [BackorderRequests] ([ClienteId]);

CREATE INDEX [IX_BackorderRequests_Estado] ON [BackorderRequests] ([Estado]);

CREATE INDEX [IX_BackorderRequests_FechaSolicitud] ON [BackorderRequests] ([FechaSolicitud]);

CREATE INDEX [IX_BackorderRequests_ProductoId_Estado] ON [BackorderRequests] ([ProductoId], [Estado]) WHERE Estado = 'Pendiente';

CREATE UNIQUE INDEX [IX_Categorias_Nombre] ON [Categorias] ([Nombre]);

CREATE UNIQUE INDEX [IX_Clientes_Email] ON [Clientes] ([Email]);

CREATE INDEX [IX_Clientes_Telefono] ON [Clientes] ([Telefono]);

CREATE INDEX [IX_DetallesPedido_EstadoLinea] ON [DetallesPedido] ([EstadoLinea]);

CREATE INDEX [IX_DetallesPedido_PedidoId] ON [DetallesPedido] ([PedidoId]);

CREATE INDEX [IX_DetallesPedido_ProductoId] ON [DetallesPedido] ([ProductoId]);

CREATE INDEX [IX_DetallesVenta_ProductoId] ON [DetallesVenta] ([ProductoId]);

CREATE INDEX [IX_DetallesVenta_VentaId] ON [DetallesVenta] ([VentaId]);

CREATE UNIQUE INDEX [IX_Empleados_Email] ON [Empleados] ([Email]);

CREATE INDEX [IX_Facturas_Fecha] ON [Facturas] ([Fecha]);

CREATE UNIQUE INDEX [IX_Facturas_Numero] ON [Facturas] ([Numero]);

CREATE INDEX [IX_Facturas_PedidoId] ON [Facturas] ([PedidoId]);

CREATE INDEX [IX_Facturas_VentaId] ON [Facturas] ([VentaId]);

CREATE INDEX [IX_MovimientosStock_Fecha] ON [MovimientosStock] ([Fecha]);

CREATE INDEX [IX_MovimientosStock_ProductoId_Fecha] ON [MovimientosStock] ([ProductoId], [Fecha]);

CREATE INDEX [IX_MovimientosStock_Tipo] ON [MovimientosStock] ([Tipo]);

CREATE INDEX [IX_NotificacionesLog_Canal] ON [NotificacionesLog] ([Canal]);

CREATE INDEX [IX_NotificacionesLog_Destinatario] ON [NotificacionesLog] ([Destinatario]);

CREATE INDEX [IX_NotificacionesLog_Estado] ON [NotificacionesLog] ([Estado]);

CREATE INDEX [IX_NotificacionesLog_FechaEnvio] ON [NotificacionesLog] ([FechaEnvio]);

CREATE INDEX [IX_NotificacionesLog_ReferenciaTipo_ReferenciaId] ON [NotificacionesLog] ([ReferenciaTipo], [ReferenciaId]);

CREATE INDEX [IX_Pedidos_ClienteId] ON [Pedidos] ([ClienteId]);

CREATE INDEX [IX_Pedidos_Estado] ON [Pedidos] ([Estado]);

CREATE INDEX [IX_Pedidos_Fecha] ON [Pedidos] ([Fecha]);

CREATE INDEX [IX_Productos_CategoriaId_Activo] ON [Productos] ([CategoriaId], [Activo]);

CREATE INDEX [IX_Productos_Nombre] ON [Productos] ([Nombre]);

CREATE INDEX [IX_Productos_StockActual] ON [Productos] ([StockActual]);

CREATE INDEX [IX_Ventas_ClienteId] ON [Ventas] ([ClienteId]);

CREATE INDEX [IX_Ventas_EmpleadoId] ON [Ventas] ([EmpleadoId]);

CREATE INDEX [IX_Ventas_Fecha] ON [Ventas] ([Fecha]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260722004507_InitialCreate', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Empleados] ADD [NumeroIdentificacion] nvarchar(50) NOT NULL DEFAULT N'';

ALTER TABLE [Clientes] ADD [NumeroIdentificacion] nvarchar(50) NOT NULL DEFAULT N'';

CREATE UNIQUE INDEX [IX_Empleados_NumeroIdentificacion] ON [Empleados] ([NumeroIdentificacion]);

CREATE UNIQUE INDEX [IX_Clientes_NumeroIdentificacion] ON [Clientes] ([NumeroIdentificacion]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260722012524_AddNumeroIdentificacion', N'10.0.10');

COMMIT;
GO

BEGIN TRANSACTION;
ALTER TABLE [Productos] ADD [CodigoBarras] nvarchar(50) NULL;

ALTER TABLE [Productos] ADD [EsCodigoGenerado] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Productos] ADD [FechaImpresionEtiqueta] datetime2 NULL;

CREATE UNIQUE INDEX [IX_Productos_CodigoBarras] ON [Productos] ([CodigoBarras]) WHERE [CodigoBarras] IS NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260724000128_AddCodigoBarrasAProducto', N'10.0.10');

COMMIT;
GO

