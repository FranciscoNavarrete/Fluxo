-- Promoción de un plan: precio promocional durante los primeros N meses, después el precio normal (monto).
ALTER TABLE mpplanes ADD COLUMN IF NOT EXISTS montopromo NUMERIC(18,2) NULL;
ALTER TABLE mpplanes ADD COLUMN IF NOT EXISTS mesespromo INTEGER NULL;

-- cobrosbase: cobros que ya había hecho la suscripción cuando empezó a regir su plan actual (-1 si el
-- primer mes se pagó a mano, para que el primer cobro de MP cuente como el mes 2).
-- cobrosrealizados: cuántos cobros aprobó MP hasta el último sync, para saber en qué mes de la promo está.
ALTER TABLE mpsuscripciones ADD COLUMN IF NOT EXISTS cobrosbase INTEGER NOT NULL DEFAULT 0;
ALTER TABLE mpsuscripciones ADD COLUMN IF NOT EXISTS cobrosrealizados INTEGER NOT NULL DEFAULT 0;

UPDATE mpsuscripciones SET cobrosbase = -1 WHERE primerpagomanual = TRUE AND cobrosbase = 0;
UPDATE mpsuscripciones SET cobrosrealizados = 1 WHERE ultimocobro IS NOT NULL AND cobrosrealizados = 0;
