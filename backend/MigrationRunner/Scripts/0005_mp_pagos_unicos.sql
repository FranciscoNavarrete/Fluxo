CREATE TABLE MpPagosUnicos (
    MpPagoUnicoId          INT IDENTITY(1,1) PRIMARY KEY,
    ClienteId              INT            NOT NULL REFERENCES Clientes(ClienteId),
    MpPlanId               INT            NOT NULL REFERENCES MpPlanes(MpPlanId),

    -- Si el cliente tenía suscripción activa y la pausó para pagar manual
    MpSuscripcionId        INT            NULL REFERENCES MpSuscripciones(MpSuscripcionId),
    FechaReanudacion       DATETIME       NULL, -- cuándo reactivar el débito automático

    -- Datos de Mercado Pago
    MpPreferenceId         NVARCHAR(100)  NOT NULL,
    MpPaymentId            NVARCHAR(100)  NULL,  -- se completa cuando llega el webhook
    InitPoint              NVARCHAR(500)  NOT NULL,

    Monto                  DECIMAL(18,2)  NOT NULL,
    Moneda                 NVARCHAR(10)   NOT NULL DEFAULT 'ARS',

    -- pending | approved | rejected | cancelled
    Estado                 NVARCHAR(50)   NOT NULL DEFAULT 'pending',

    FechaCreacion          DATETIME       NOT NULL DEFAULT GETUTCDATE(),
    FechaPago              DATETIME       NULL
);
GO

CREATE INDEX IX_MpPagosUnicos_ClienteId      ON MpPagosUnicos(ClienteId);
CREATE INDEX IX_MpPagosUnicos_MpPreferenceId ON MpPagosUnicos(MpPreferenceId);
GO
