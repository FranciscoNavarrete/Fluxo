-- ============================================================
-- Migración 0002: Auth standalone — tabla Usuarios
-- Extiende Clientes con Apellido y Telefono
-- ============================================================

-- ── Extender Clientes si le faltan columnas ──────────────────
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Clientes') AND name = 'Apellido')
    ALTER TABLE Clientes ADD Apellido NVARCHAR(200) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Clientes') AND name = 'Telefono')
    ALTER TABLE Clientes ADD Telefono NVARCHAR(30) NULL;
GO

-- ── Tabla Usuarios ────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Usuarios')
BEGIN
    CREATE TABLE Usuarios
    (
        UsuarioId               INT             NOT NULL IDENTITY(1,1),
        Email                   NVARCHAR(256)   NOT NULL,
        PasswordHash            NVARCHAR(256)   NOT NULL,
        Rol                     NVARCHAR(20)    NOT NULL CONSTRAINT DF_Usuarios_Rol DEFAULT ('CLIENTE'),
        ClienteId               INT                 NULL,   -- NULL para admin/sistema
        Activo                  BIT             NOT NULL CONSTRAINT DF_Usuarios_Activo DEFAULT (1),
        FechaHoraCreacion       DATETIME2       NOT NULL CONSTRAINT DF_Usuarios_Fecha DEFAULT (GETUTCDATE()),

        CONSTRAINT PK_Usuarios          PRIMARY KEY CLUSTERED (UsuarioId),
        CONSTRAINT UQ_Usuarios_Email    UNIQUE (Email),
        CONSTRAINT FK_Usuarios_Cliente  FOREIGN KEY (ClienteId) REFERENCES Clientes(ClienteId),
        CONSTRAINT CK_Usuarios_Rol      CHECK (Rol IN ('ADMINISTRADOR', 'SISTEMA', 'CLIENTE'))
    );

    -- Admin por defecto (password: Admin1234!)
    -- Hash generado con BCrypt cost=12
    INSERT INTO Usuarios (Email, PasswordHash, Rol, ClienteId)
    VALUES ('admin@credigial.com',
            '$2a$12$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2uheWG/igi.',
            'ADMINISTRADOR', NULL);

    PRINT 'Tabla Usuarios creada con usuario admin por defecto.';
END
ELSE
    PRINT 'Tabla Usuarios ya existe — omitida.';
GO

PRINT '=== Migración 0002_auth_usuarios completada. ===';
GO
