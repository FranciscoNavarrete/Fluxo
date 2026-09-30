CREATE TABLE IF NOT EXISTS clientes (
    clienteid           SERIAL          NOT NULL,
    nombre              VARCHAR(200)    NOT NULL,
    email               VARCHAR(256)    NOT NULL,
    activo              BOOLEAN         NOT NULL DEFAULT TRUE,
    fechahoracreacion   TIMESTAMP       NOT NULL DEFAULT NOW(),

    CONSTRAINT pk_clientes       PRIMARY KEY (clienteid),
    CONSTRAINT uq_clientes_email UNIQUE (email)
);

INSERT INTO clientes (nombre, email)
SELECT 'Cliente Demo 1', 'demo1@ejemplo.com'
WHERE NOT EXISTS (SELECT 1 FROM clientes WHERE email = 'demo1@ejemplo.com');

INSERT INTO clientes (nombre, email)
SELECT 'Cliente Demo 2', 'demo2@ejemplo.com'
WHERE NOT EXISTS (SELECT 1 FROM clientes WHERE email = 'demo2@ejemplo.com');
