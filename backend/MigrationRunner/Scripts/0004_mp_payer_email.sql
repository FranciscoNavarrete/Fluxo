-- Agrega el email real de MP del pagador, capturado desde el webhook cuando la suscripción se autoriza
ALTER TABLE MpSuscripciones
ADD MpPayerEmail NVARCHAR(255) NULL;
GO
