using System.Data;
using System.Reflection;
using Dapper;
using Dapper.Contrib.Extensions;

namespace DataAccess;

/// <summary>
/// Reemplazo de Dapper.Contrib's InsertAsync/UpdateAsync para Postgres. El PostgresAdapter de
/// Dapper.Contrib cita cada columna con el nombre exacto de la propiedad C# (ej. "Email"), y en
/// Postgres un identificador citado es case-sensitive -- como las tablas se crearon con columnas
/// en minúscula sin comillas, todo INSERT/UPDATE generado por Contrib falla con
/// "column \"Email\" of relation \"usuarios\" does not exist". No hay forma pública de
/// reemplazar el adapter registrado (el diccionario es privado y sus métodos son
/// virtual+final, no overrideables), así que se arma el SQL a mano igual que el resto de las
/// queries de este proyecto: nombres en minúscula sin comillas.
/// </summary>
internal static class PostgresDapperExtensions
{
    public static async Task<int> InsertLowercaseAsync<T>(this IDbConnection connection, T entity)
        where T : notnull
    {
        var (table, keyColumn, columns) = Describe<T>();

        var columnList = string.Join(", ", columns.Select(p => p.Name.ToLowerInvariant()));
        var paramList = string.Join(", ", columns.Select(p => "@" + p.Name));
        var sql = $"INSERT INTO {table} ({columnList}) VALUES ({paramList}) RETURNING {keyColumn}";

        return await connection.ExecuteScalarAsync<int>(sql, entity);
    }

    public static async Task<bool> UpdateLowercaseAsync<T>(this IDbConnection connection, T entity)
        where T : notnull
    {
        var (table, keyColumn, columns) = Describe<T>();
        var keyProp = typeof(T).GetProperties().Single(p => p.GetCustomAttribute<KeyAttribute>() != null);

        var setClause = string.Join(", ", columns.Select(p => $"{p.Name.ToLowerInvariant()} = @{p.Name}"));
        var sql = $"UPDATE {table} SET {setClause} WHERE {keyColumn} = @{keyProp.Name}";

        var filas = await connection.ExecuteAsync(sql, entity);
        return filas > 0;
    }

    private static (string Table, string KeyColumn, PropertyInfo[] Columns) Describe<T>()
    {
        var type = typeof(T);
        var tableAttr = type.GetCustomAttribute<TableAttribute>();
        var table = (tableAttr?.Name ?? type.Name).ToLowerInvariant();

        var keyProp = type.GetProperties().Single(p => p.GetCustomAttribute<KeyAttribute>() != null);
        var columns = type.GetProperties().Where(p => p != keyProp).ToArray();

        return (table, keyProp.Name.ToLowerInvariant(), columns);
    }
}
