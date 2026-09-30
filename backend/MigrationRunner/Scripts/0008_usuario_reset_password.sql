ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS resetpasswordtoken      VARCHAR(100);
ALTER TABLE usuarios ADD COLUMN IF NOT EXISTS resetpasswordtokenexpiry TIMESTAMP;
