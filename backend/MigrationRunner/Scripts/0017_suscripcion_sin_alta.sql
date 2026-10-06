-- Suscripción nueva para un negocio que ya existía: cobra solo el abono, sin el alta del primer cobro.
ALTER TABLE mpsuscripciones ADD COLUMN IF NOT EXISTS sinalta BOOLEAN NOT NULL DEFAULT FALSE;
