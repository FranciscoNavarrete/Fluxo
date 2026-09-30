CREATE TABLE IF NOT EXISTS mppagosunicos (
    mppagounicoid       SERIAL          NOT NULL,
    clienteid           INT             NOT NULL,
    mpplanid            INT             NOT NULL,
    mpsuscripcionid     INT,
    fechareanudacion    TIMESTAMP,
    mppreferenceid      VARCHAR(100)    NOT NULL,
    mppaymentid         VARCHAR(100),
    initpoint           VARCHAR(500)    NOT NULL,
    monto               DECIMAL(18,2)   NOT NULL,
    moneda              VARCHAR(10)     NOT NULL DEFAULT 'ARS',
    estado              VARCHAR(50)     NOT NULL DEFAULT 'pending',
    fechacreacion       TIMESTAMP       NOT NULL DEFAULT NOW(),
    fechapago           TIMESTAMP,

    CONSTRAINT pk_mppagosunicos             PRIMARY KEY (mppagounicoid),
    CONSTRAINT fk_mppagosunicos_cliente     FOREIGN KEY (clienteid)       REFERENCES clientes(clienteid),
    CONSTRAINT fk_mppagosunicos_plan        FOREIGN KEY (mpplanid)        REFERENCES mpplanes(mpplanid),
    CONSTRAINT fk_mppagosunicos_suscripcion FOREIGN KEY (mpsuscripcionid) REFERENCES mpsuscripciones(mpsuscripcionid)
);

CREATE INDEX IF NOT EXISTS ix_mppagosunicos_clienteid     ON mppagosunicos (clienteid);
CREATE INDEX IF NOT EXISTS ix_mppagosunicos_mppreferenceid ON mppagosunicos (mppreferenceid);
