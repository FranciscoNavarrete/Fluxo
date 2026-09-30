ALTER TABLE clientes ADD COLUMN IF NOT EXISTS apellido  VARCHAR(200);
ALTER TABLE clientes ADD COLUMN IF NOT EXISTS telefono  VARCHAR(30);

CREATE TABLE IF NOT EXISTS usuarios (
    usuarioid               SERIAL          NOT NULL,
    email                   VARCHAR(256)    NOT NULL,
    passwordhash            VARCHAR(256)    NOT NULL,
    rol                     VARCHAR(20)     NOT NULL DEFAULT 'CLIENTE',
    clienteid               INT,
    activo                  BOOLEAN         NOT NULL DEFAULT TRUE,
    fechahoracreacion       TIMESTAMP       NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_usuarios         PRIMARY KEY (usuarioid),
    CONSTRAINT uq_usuarios_email   UNIQUE (email),
    CONSTRAINT fk_usuarios_cliente FOREIGN KEY (clienteid) REFERENCES clientes(clienteid),
    CONSTRAINT ck_usuarios_rol     CHECK (rol IN ('ADMINISTRADOR', 'SISTEMA', 'CLIENTE'))
);

-- Admin por defecto (password: Admin1234!)
INSERT INTO usuarios (email, passwordhash, rol, clienteid)
SELECT 'admin@fluxo.com',
       '$2a$12$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2uheWG/igi.',
       'ADMINISTRADOR', NULL
WHERE NOT EXISTS (SELECT 1 FROM usuarios WHERE email = 'admin@fluxo.com');
