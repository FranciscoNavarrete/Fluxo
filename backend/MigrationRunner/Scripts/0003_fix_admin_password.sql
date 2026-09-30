-- Fix: actualizar hash de contraseña del admin (Admin1234!)
UPDATE Usuarios
SET PasswordHash = '$2a$12$lX9C/d11T6D0UZvsdTNU6uBSX6zKBifrhPPnhnLAivF.XUcZmGzzG'
WHERE Email = 'admin@credigial.com';
GO
