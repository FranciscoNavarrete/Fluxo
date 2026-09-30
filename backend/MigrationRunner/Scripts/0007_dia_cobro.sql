-- Día del mes en que se prefiere cobrar (1-31). NULL = cobrar según la frecuencia del plan.
ALTER TABLE MpSuscripciones
ADD DiaCobro TINYINT NULL;
GO
