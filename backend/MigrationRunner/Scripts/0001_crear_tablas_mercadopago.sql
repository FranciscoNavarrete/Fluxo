-- ── MpPlanes ─────────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS mpplanes (
    mpplanid                    SERIAL          NOT NULL,
    nombre                      VARCHAR(100)    NOT NULL,
    descripcion                 VARCHAR(500),
    monto                       DECIMAL(18,2)   NOT NULL,
    moneda                      VARCHAR(10)     NOT NULL DEFAULT 'ARS',
    tipofrecuencia              VARCHAR(10)     NOT NULL,
    frecuencia                  INT             NOT NULL,
    diasgratis                  INT             NOT NULL DEFAULT 0,
    mpplanexternoid             VARCHAR(100),
    activo                      BOOLEAN         NOT NULL DEFAULT TRUE,
    usuariocreacionid           INT             NOT NULL,
    fechahoracreacion           TIMESTAMP       NOT NULL DEFAULT NOW(),
    usuarioultatualizacionid    INT,
    fechahoraultactualizacion   TIMESTAMP,

    CONSTRAINT pk_mpplanes                PRIMARY KEY (mpplanid),
    CONSTRAINT ck_mpplanes_monto          CHECK (monto > 0),
    CONSTRAINT ck_mpplanes_frecuencia     CHECK (frecuencia > 0),
    CONSTRAINT ck_mpplanes_tipofrecuencia CHECK (tipofrecuencia IN ('months', 'days'))
);

CREATE INDEX IF NOT EXISTS ix_mpplanes_activo         ON mpplanes (activo);
CREATE INDEX IF NOT EXISTS ix_mpplanes_mpplanexternoid ON mpplanes (mpplanexternoid) WHERE mpplanexternoid IS NOT NULL;

-- ── MpSuscripciones ───────────────────────────────────────────
CREATE TABLE IF NOT EXISTS mpsuscripciones (
    mpsuscripcionid             SERIAL          NOT NULL,
    clienteid                   INT             NOT NULL,
    mpplanid                    INT             NOT NULL,
    gatewaysuscripcionid        VARCHAR(100)    NOT NULL,
    gatewayproveedor            VARCHAR(50)     NOT NULL DEFAULT 'MercadoPago',
    mppayerid                   VARCHAR(100),
    estado                      VARCHAR(20)     NOT NULL DEFAULT 'pending',
    fechainicio                 TIMESTAMP       NOT NULL,
    proximocobro                TIMESTAMP,
    ultimocobro                 TIMESTAMP,
    fechasuspension             TIMESTAMP,
    fechacancelacion            TIMESTAMP,
    motivocancelacion           VARCHAR(500),
    intentosreintento           INT             NOT NULL DEFAULT 0,
    maxreintentos               INT             NOT NULL DEFAULT 3,
    consentimientofecha         TIMESTAMP,
    consentimientoip            VARCHAR(45),
    consentimientouseragent     VARCHAR(500),
    terminosversion             VARCHAR(20),
    usuariocreacionid           INT             NOT NULL,
    fechahoracreacion           TIMESTAMP       NOT NULL DEFAULT NOW(),
    usuarioultatualizacionid    INT,
    fechahoraultactualizacion   TIMESTAMP,

    CONSTRAINT pk_mpsuscripciones            PRIMARY KEY (mpsuscripcionid),
    CONSTRAINT uq_mpsuscripciones_gatewayid  UNIQUE (gatewaysuscripcionid),
    CONSTRAINT fk_mpsuscripciones_cliente    FOREIGN KEY (clienteid) REFERENCES clientes(clienteid),
    CONSTRAINT fk_mpsuscripciones_plan       FOREIGN KEY (mpplanid)  REFERENCES mpplanes(mpplanid),
    CONSTRAINT ck_mpsuscripciones_estado     CHECK (estado IN (
        'pending', 'authorized', 'paused', 'suspended', 'cancelled'
    ))
);

CREATE INDEX IF NOT EXISTS ix_mpsuscripciones_clienteid     ON mpsuscripciones (clienteid);
CREATE INDEX IF NOT EXISTS ix_mpsuscripciones_estado        ON mpsuscripciones (estado);
CREATE INDEX IF NOT EXISTS ix_mpsuscripciones_dunninglookup ON mpsuscripciones (estado, proximocobro)
    WHERE estado IN ('authorized', 'paused');

-- ── MpTransacciones ───────────────────────────────────────────
CREATE TABLE IF NOT EXISTS mptransacciones (
    mptransaccionid             SERIAL          NOT NULL,
    mpsuscripcionid             INT             NOT NULL,
    gatewaypagoid               VARCHAR(100)    NOT NULL,
    monto                       DECIMAL(18,2)   NOT NULL,
    moneda                      VARCHAR(10)     NOT NULL DEFAULT 'ARS',
    estado                      VARCHAR(30)     NOT NULL,
    estadodetalle               VARCHAR(100),
    numerointento               INT             NOT NULL DEFAULT 1,
    fechaprocesado              TIMESTAMP       NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_mptransacciones             PRIMARY KEY (mptransaccionid),
    CONSTRAINT uq_mptransacciones_gatewaypago UNIQUE (gatewaypagoid),
    CONSTRAINT fk_mptransacciones_suscripcion FOREIGN KEY (mpsuscripcionid) REFERENCES mpsuscripciones(mpsuscripcionid),
    CONSTRAINT ck_mptransacciones_monto       CHECK (monto >= 0)
);

CREATE INDEX IF NOT EXISTS ix_mptransacciones_suscripcionid ON mptransacciones (mpsuscripcionid);
CREATE INDEX IF NOT EXISTS ix_mptransacciones_estado        ON mptransacciones (estado);

-- ── MpDunningLogs ─────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS mpdunninglogs (
    mpdunninglogid              SERIAL          NOT NULL,
    mpsuscripcionid             INT             NOT NULL,
    accion                      VARCHAR(20)     NOT NULL,
    diasvencido                 INT             NOT NULL,
    canal                       VARCHAR(50)     NOT NULL,
    fechaejecucion              TIMESTAMP       NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_mpdunninglogs             PRIMARY KEY (mpdunninglogid),
    CONSTRAINT fk_mpdunninglogs_suscripcion FOREIGN KEY (mpsuscripcionid) REFERENCES mpsuscripciones(mpsuscripcionid),
    CONSTRAINT ck_mpdunninglogs_accion      CHECK (accion IN ('email', 'whatsapp', 'warning', 'suspend', 'cancel'))
);

CREATE INDEX IF NOT EXISTS ix_mpdunninglogs_suscripcionid    ON mpdunninglogs (mpsuscripcionid);
CREATE INDEX IF NOT EXISTS ix_mpdunninglogs_suscripcionfecha ON mpdunninglogs (mpsuscripcionid, fechaejecucion);
