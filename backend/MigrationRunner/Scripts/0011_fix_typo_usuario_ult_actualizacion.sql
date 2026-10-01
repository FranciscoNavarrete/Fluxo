-- Typo original: "ultatualizacion" (falta la "c" de "actualizacion"). Quedó enmascarado
-- hasta ahora porque Dapper.Contrib generaba UPDATE inválidos contra Postgres (columnas
-- citadas en PascalCase) y nunca llegó a ejecutarse una sentencia real contra esta columna.
ALTER TABLE mpplanes        RENAME COLUMN usuarioultatualizacionid TO usuarioultactualizacionid;
ALTER TABLE mpsuscripciones RENAME COLUMN usuarioultatualizacionid TO usuarioultactualizacionid;
