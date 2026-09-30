ALTER TABLE mppagosunicos ADD COLUMN IF NOT EXISTS externalreference VARCHAR(100);

CREATE INDEX IF NOT EXISTS ix_mppagosunicos_externalreference ON mppagosunicos (externalreference);
