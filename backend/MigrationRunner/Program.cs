using System.Reflection;
using DbUp;
using Microsoft.Extensions.Configuration;

// Conexión: argumento CLI > variable de entorno > appsettings.json
var config = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var connectionString = args.Length > 0
    ? args[0]
    : config.GetConnectionString("DefaultConnection")
      ?? throw new InvalidOperationException(
            "Especificá la cadena de conexión como argumento o en ConnectionStrings:DefaultConnection.");

Console.WriteLine($"Conectando a: {MaskPassword(connectionString)}");

// Crear la base de datos si no existe
EnsureDatabase.For.PostgresqlDatabase(connectionString);

// Ejecutar scripts embebidos en orden alfanumérico
var upgrader = DeployChanges.To
    .PostgresqlDatabase(connectionString)
    .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
    .WithVariablesDisabled()   // evita que $ en strings SQL sean tratados como variables
    .LogToConsole()
    .Build();

if (upgrader.IsUpgradeRequired())
{
    var result = upgrader.PerformUpgrade();

    if (!result.Successful)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Error.WriteLine($"Migración fallida: {result.Error}");
        Console.ResetColor();
        return 1;
    }
}
else
{
    Console.WriteLine("La base de datos ya está actualizada.");
}

Console.ForegroundColor = ConsoleColor.Green;
Console.WriteLine("Migraciones completadas exitosamente.");
Console.ResetColor();
return 0;

static string MaskPassword(string connStr)
{
    // Oculta el valor de Password= en el log
    return System.Text.RegularExpressions.Regex.Replace(
        connStr, @"(Password|pwd)=([^;]*)", "$1=***", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
}
