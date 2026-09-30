-- ============================================================
-- Prerrequisito: tabla Clientes mínima para que funcionen las FKs.
-- En el sistema existente esta tabla ya existe — este script es
-- solo para la solución standalone / entorno de desarrollo.
-- ============================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Clientes')
BEGIN
    CREATE TABLE Clientes
    (
        ClienteId                   INT             NOT NULL IDENTITY(1,1),
        Nombre                      NVARCHAR(200)   NOT NULL,
        Email                       NVARCHAR(256)   NOT NULL,
        Activo                      BIT             NOT NULL CONSTRAINT DF_Clientes_Activo DEFAULT (1),
        FechaHoraCreacion           DATETIME2       NOT NULL CONSTRAINT DF_Clientes_FechaCreacion DEFAULT (GETUTCDATE()),

        CONSTRAINT PK_Clientes       PRIMARY KEY CLUSTERED (ClienteId),
        CONSTRAINT UQ_Clientes_Email UNIQUE (Email)
    );

    -- Datos de prueba
    INSERT INTO Clientes (Nombre, Email) VALUES
        ('Cliente Demo 1', 'demo1@ejemplo.com'),
        ('Cliente Demo 2', 'demo2@ejemplo.com');

    PRINT 'Tabla Clientes (stub) creada con datos de prueba.';
END
ELSE
    PRINT 'Tabla Clientes ya existe — omitida.';
GO
