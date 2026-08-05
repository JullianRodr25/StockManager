BEGIN TRANSACTION;
ALTER TABLE [Productos] ADD [TarifaIva] decimal(5,2) NOT NULL DEFAULT 19.0;

CREATE TABLE [Configuracion] (
    [Id] int NOT NULL IDENTITY,
    [TarifaIvaPorDefecto] decimal(5,2) NOT NULL,
    CONSTRAINT [PK_Configuracion] PRIMARY KEY ([Id])
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'TarifaIvaPorDefecto') AND [object_id] = OBJECT_ID(N'[Configuracion]'))
    SET IDENTITY_INSERT [Configuracion] ON;
INSERT INTO [Configuracion] ([Id], [TarifaIvaPorDefecto])
VALUES (1, 19.0);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'TarifaIvaPorDefecto') AND [object_id] = OBJECT_ID(N'[Configuracion]'))
    SET IDENTITY_INSERT [Configuracion] OFF;

ALTER TABLE [Productos] ADD CONSTRAINT [CK_Producto_TarifaIva_Between_Zero_And_OneHundred] CHECK ([TarifaIva] >= 0 AND [TarifaIva] <= 100);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260804234640_AddConfiguracionIva', N'10.0.10');

COMMIT;
GO

