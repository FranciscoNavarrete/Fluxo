-- ============================================================
-- Migración: Módulo de Cobros Recurrentes — Mercado Pago
-- Tablas: MpPlanes, MpSuscripciones, MpTransacciones, MpDunningLogs
-- Requiere: tabla Clientes (Clientes.ClienteId INT PK)
-- ============================================================

-- ============================================================
-- 1. MpPlanes
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MpPlanes')
BEGIN
    CREATE TABLE MpPlanes
    (
        MpPlanId                    INT             NOT NULL IDENTITY(1,1),
        Nombre                      NVARCHAR(100)   NOT NULL,
        Descripcion                 NVARCHAR(500)       NULL,
        Monto                       DECIMAL(18,2)   NOT NULL,
        Moneda                      NVARCHAR(10)    NOT NULL CONSTRAINT DF_MpPlanes_Moneda DEFAULT ('ARS'),
        TipoFrecuencia              NVARCHAR(10)    NOT NULL, -- 'months' | 'days'
        Frecuencia                  INT             NOT NULL,
        DiasGratis                  INT             NOT NULL CONSTRAINT DF_MpPlanes_DiasGratis DEFAULT (0),
        MpPlanExternoId             NVARCHAR(100)       NULL,
        Activo                      BIT             NOT NULL CONSTRAINT DF_MpPlanes_Activo DEFAULT (1),

        -- Auditoría
        UsuarioCreacionId           INT             NOT NULL,
        FechaHoraCreacion           DATETIME2       NOT NULL CONSTRAINT DF_MpPlanes_FechaCreacion DEFAULT (GETUTCDATE()),
        UsuarioUltActualizacionId   INT                 NULL,
        FechaHoraUltActualizacion   DATETIME2           NULL,

        CONSTRAINT PK_MpPlanes PRIMARY KEY CLUSTERED (MpPlanId),
        CONSTRAINT CK_MpPlanes_Monto          CHECK (Monto > 0),
        CONSTRAINT CK_MpPlanes_Frecuencia     CHECK (Frecuencia > 0),
        CONSTRAINT CK_MpPlanes_TipoFrecuencia CHECK (TipoFrecuencia IN ('months', 'days'))
    );

    CREATE NONCLUSTERED INDEX IX_MpPlanes_Activo
        ON MpPlanes (Activo)
        INCLUDE (Nombre, Monto, Moneda, TipoFrecuencia, Frecuencia);

    CREATE NONCLUSTERED INDEX IX_MpPlanes_MpPlanExternoId
        ON MpPlanes (MpPlanExternoId)
        WHERE MpPlanExternoId IS NOT NULL;

    PRINT 'Tabla MpPlanes creada.';
END
ELSE
    PRINT 'Tabla MpPlanes ya existe — omitida.';
GO

-- ============================================================
-- 2. MpSuscripciones
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MpSuscripciones')
BEGIN
    CREATE TABLE MpSuscripciones
    (
        MpSuscripcionId             INT             NOT NULL IDENTITY(1,1),
        ClienteId                   INT             NOT NULL,
        MpPlanId                    INT             NOT NULL,
        GatewaySuscripcionId        NVARCHAR(100)   NOT NULL,   -- pre_approval_id de MP
        GatewayProveedor            NVARCHAR(50)    NOT NULL CONSTRAINT DF_MpSuscripciones_Proveedor DEFAULT ('MercadoPago'),
        MpPayerId                   NVARCHAR(100)       NULL,
        Estado                      NVARCHAR(20)    NOT NULL CONSTRAINT DF_MpSuscripciones_Estado DEFAULT ('pending'),
        FechaInicio                 DATETIME2       NOT NULL,
        ProximoCobro                DATETIME2           NULL,
        UltimoCobro                 DATETIME2           NULL,
        FechaSuspension             DATETIME2           NULL,
        FechaCancelacion            DATETIME2           NULL,
        MotivoCancelacion           NVARCHAR(500)       NULL,
        IntentosReintento           INT             NOT NULL CONSTRAINT DF_MpSuscripciones_Intentos DEFAULT (0),
        MaxReintentos               INT             NOT NULL CONSTRAINT DF_MpSuscripciones_MaxReintentos DEFAULT (3),

        -- Mandato de débito
        ConsentimientoFecha         DATETIME2           NULL,
        ConsentimientoIp            NVARCHAR(45)        NULL,   -- IPv4 o IPv6
        ConsentimientoUserAgent     NVARCHAR(500)       NULL,
        TerminosVersion             NVARCHAR(20)        NULL,

        -- Auditoría
        UsuarioCreacionId           INT             NOT NULL,
        FechaHoraCreacion           DATETIME2       NOT NULL CONSTRAINT DF_MpSuscripciones_FechaCreacion DEFAULT (GETUTCDATE()),
        UsuarioUltActualizacionId   INT                 NULL,
        FechaHoraUltActualizacion   DATETIME2           NULL,

        CONSTRAINT PK_MpSuscripciones            PRIMARY KEY CLUSTERED (MpSuscripcionId),
        CONSTRAINT UQ_MpSuscripciones_GatewayId  UNIQUE (GatewaySuscripcionId),
        CONSTRAINT FK_MpSuscripciones_Cliente     FOREIGN KEY (ClienteId)  REFERENCES Clientes(ClienteId),
        CONSTRAINT FK_MpSuscripciones_Plan        FOREIGN KEY (MpPlanId)   REFERENCES MpPlanes(MpPlanId),
        CONSTRAINT CK_MpSuscripciones_Estado      CHECK (Estado IN (
            'pending', 'authorized', 'paused', 'suspended', 'cancelled'
        ))
    );

    CREATE NONCLUSTERED INDEX IX_MpSuscripciones_ClienteId
        ON MpSuscripciones (ClienteId)
        INCLUDE (Estado, MpPlanId, GatewaySuscripcionId);

    CREATE NONCLUSTERED INDEX IX_MpSuscripciones_Estado
        ON MpSuscripciones (Estado)
        INCLUDE (ClienteId, MpPlanId, ProximoCobro);

    -- Índice para dunning: suscripciones vencidas con estado activo
    CREATE NONCLUSTERED INDEX IX_MpSuscripciones_DunningLookup
        ON MpSuscripciones (Estado, ProximoCobro)
        WHERE Estado IN ('authorized', 'paused');

    PRINT 'Tabla MpSuscripciones creada.';
END
ELSE
    PRINT 'Tabla MpSuscripciones ya existe — omitida.';
GO

-- ============================================================
-- 3. MpTransacciones
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MpTransacciones')
BEGIN
    CREATE TABLE MpTransacciones
    (
        MpTransaccionId             INT             NOT NULL IDENTITY(1,1),
        MpSuscripcionId             INT             NOT NULL,
        GatewayPagoId               NVARCHAR(100)   NOT NULL,   -- payment_id de MP
        Monto                       DECIMAL(18,2)   NOT NULL,
        Moneda                      NVARCHAR(10)    NOT NULL CONSTRAINT DF_MpTransacciones_Moneda DEFAULT ('ARS'),
        Estado                      NVARCHAR(30)    NOT NULL,   -- approved | rejected | pending | cancelled
        EstadoDetalle               NVARCHAR(100)       NULL,
        NumeroIntento               INT             NOT NULL CONSTRAINT DF_MpTransacciones_Intento DEFAULT (1),
        FechaProcesado              DATETIME2       NOT NULL CONSTRAINT DF_MpTransacciones_Fecha DEFAULT (GETUTCDATE()),

        CONSTRAINT PK_MpTransacciones             PRIMARY KEY CLUSTERED (MpTransaccionId),
        CONSTRAINT UQ_MpTransacciones_GatewayPago UNIQUE (GatewayPagoId),
        CONSTRAINT FK_MpTransacciones_Suscripcion FOREIGN KEY (MpSuscripcionId) REFERENCES MpSuscripciones(MpSuscripcionId),
        CONSTRAINT CK_MpTransacciones_Monto       CHECK (Monto >= 0)
    );

    CREATE NONCLUSTERED INDEX IX_MpTransacciones_SuscripcionId
        ON MpTransacciones (MpSuscripcionId)
        INCLUDE (Estado, Monto, FechaProcesado);

    CREATE NONCLUSTERED INDEX IX_MpTransacciones_Estado
        ON MpTransacciones (Estado)
        INCLUDE (MpSuscripcionId, FechaProcesado);

    PRINT 'Tabla MpTransacciones creada.';
END
ELSE
    PRINT 'Tabla MpTransacciones ya existe — omitida.';
GO

-- ============================================================
-- 4. MpDunningLogs
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MpDunningLogs')
BEGIN
    CREATE TABLE MpDunningLogs
    (
        MpDunningLogId              INT             NOT NULL IDENTITY(1,1),
        MpSuscripcionId             INT             NOT NULL,
        Accion                      NVARCHAR(20)    NOT NULL,   -- email | whatsapp | warning | suspend | cancel
        DiasVencido                 INT             NOT NULL,
        Canal                       NVARCHAR(50)    NOT NULL,
        FechaEjecucion              DATETIME2       NOT NULL CONSTRAINT DF_MpDunningLogs_Fecha DEFAULT (GETUTCDATE()),

        CONSTRAINT PK_MpDunningLogs             PRIMARY KEY CLUSTERED (MpDunningLogId),
        CONSTRAINT FK_MpDunningLogs_Suscripcion FOREIGN KEY (MpSuscripcionId) REFERENCES MpSuscripciones(MpSuscripcionId),
        CONSTRAINT CK_MpDunningLogs_Accion      CHECK (Accion IN ('email', 'whatsapp', 'warning', 'suspend', 'cancel'))
    );

    CREATE NONCLUSTERED INDEX IX_MpDunningLogs_SuscripcionId
        ON MpDunningLogs (MpSuscripcionId)
        INCLUDE (Accion, FechaEjecucion);

    -- Índice para la verificación de idempotencia diaria del dunning
    CREATE NONCLUSTERED INDEX IX_MpDunningLogs_SuscripcionFecha
        ON MpDunningLogs (MpSuscripcionId, FechaEjecucion);

    PRINT 'Tabla MpDunningLogs creada.';
END
ELSE
    PRINT 'Tabla MpDunningLogs ya existe — omitida.';
GO

PRINT '=== Migración 0001_crear_tablas_mercadopago completada. ===';
GO
