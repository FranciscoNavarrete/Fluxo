#!/bin/zsh
# Levanta todo el entorno de desarrollo: Colima + SQL Server + ngrok + API

export PATH="/opt/homebrew/bin:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$PATH:$HOME/.dotnet"

PROYECTO="/Users/francisco/Documents/Proyectos/back-end"

echo "→ Iniciando Colima..."
if ! colima status 2>/dev/null | grep -q "Running"; then
  colima start --memory 4 --arch x86_64
else
  echo "  Colima ya está corriendo."
fi

echo "→ Iniciando SQL Server..."
if ! docker ps | grep -q sqlserver; then
  docker start sqlserver 2>/dev/null || \
  docker run -d --name sqlserver \
    -e ACCEPT_EULA=Y \
    -e SA_PASSWORD=CobrosRecurrentes2024! \
    -p 1433:1433 \
    mcr.microsoft.com/mssql/server:2022-latest
  sleep 8
else
  echo "  SQL Server ya está corriendo."
fi

echo "→ Iniciando ngrok..."
pkill ngrok 2>/dev/null
ngrok start api --log=stdout > /tmp/ngrok.log 2>&1 &
sleep 3
echo "  URL pública: https://glove-calculus-outright.ngrok-free.dev"

echo "→ Iniciando API..."
pkill -f "CobrosRecurrentes.WebApp" 2>/dev/null
sleep 1
dotnet run --project "$PROYECTO/WebApp/CobrosRecurrentes.WebApp.csproj" \
  --no-build > /tmp/webapi.log 2>&1 &
sleep 5

if grep -q "Now listening" /tmp/webapi.log; then
  echo ""
  echo "✓ Todo listo:"
  echo "  API:     http://localhost:5050"
  echo "  Swagger: http://localhost:5050/swagger"
  echo "  Público: https://glove-calculus-outright.ngrok-free.dev"
  echo "  Logs:    tail -f /tmp/webapi.log"
else
  echo "✗ Error al iniciar la API. Revisá: tail -f /tmp/webapi.log"
fi
