ALTER TABLE Usuarios
    ADD ResetPasswordToken     NVARCHAR(100) NULL,
        ResetPasswordTokenExpiry DATETIME2    NULL;
