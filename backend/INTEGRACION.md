# Integración: Módulo Cobros Recurrentes — Mercado Pago

## NuGet package a agregar

```xml
<!-- En el proyecto BusinessLogic y WebApp -->
<PackageReference Include="mercadopago-sdk" Version="2.4.1" />
```

---

## Program.cs — líneas a agregar

```csharp
// Después de builder.Services.AddBusinessLogicServices() o similar:
builder.Services.AddMercadoPagoServices(builder.Configuration);

// Registrar la implementación concreta de notificaciones (la tuya):
builder.Services.AddScoped<IDunningNotificador, TuImplementacionDunningNotificador>();
```

---

## appsettings.json — sección a agregar

```json
{
  "MercadoPago": {
    "AccessToken":   "APP_USR-xxxxxxxxxxxx",
    "PublicKey":     "APP_USR-xxxxxxxxxxxx",
    "WebhookSecret": "whsec_xxxxxxxxxxxx",
    "BackUrl":       "https://tu-dominio.com/suscripciones/resultado"
  }
}
```

En producción los valores vienen de **AWS Secrets Manager** — el sistema ya lo maneja.

---

## Cambios en archivos existentes

### IUnitOfWork.cs
```csharp
// Agregar al final de la interfaz:
IMpPlanRepository        MpPlan        { get; }
IMpSuscripcionRepository MpSuscripcion { get; }
IMpTransaccionRepository MpTransaccion { get; }
IMpDunningLogRepository  MpDunningLog  { get; }
```

### DataAccessUnitOfWork.cs
```csharp
// Campos privados:
private IMpPlanRepository?        _mpPlan;
private IMpSuscripcionRepository? _mpSuscripcion;
private IMpTransaccionRepository? _mpTransaccion;
private IMpDunningLogRepository?  _mpDunningLog;

// Propiedades públicas (lazy init con la misma IDbConnection):
public IMpPlanRepository        MpPlan        => _mpPlan        ??= new MpPlanRepository(_connection);
public IMpSuscripcionRepository MpSuscripcion => _mpSuscripcion ??= new MpSuscripcionRepository(_connection);
public IMpTransaccionRepository MpTransaccion => _mpTransaccion ??= new MpTransaccionRepository(_connection);
public IMpDunningLogRepository  MpDunningLog  => _mpDunningLog  ??= new MpDunningLogRepository(_connection);
```

---

## Migración de base de datos

Copiar `MigrationRunner/Scripts/0001_crear_tablas_mercadopago.sql`
al proyecto MigrationRunner como **Embedded Resource** y ejecutar con dbup.

---

## Endpoints expuestos

| Método | Ruta                                        | Auth                    | Descripción                              |
|--------|---------------------------------------------|-------------------------|------------------------------------------|
| GET    | /api/mp/planes                              | SISTEMA, ADMINISTRADOR  | Lista planes activos                     |
| GET    | /api/mp/planes/{id}                         | SISTEMA, ADMINISTRADOR  | Obtiene un plan por ID                   |
| POST   | /api/mp/planes                              | SISTEMA, ADMINISTRADOR  | Crea plan (también en MP)                |
| PUT    | /api/mp/planes                              | SISTEMA, ADMINISTRADOR  | Edita plan                               |
| DELETE | /api/mp/planes/{id}                         | SISTEMA, ADMINISTRADOR  | Baja lógica del plan                     |
| GET    | /api/mp/suscripciones                       | SISTEMA, ADMINISTRADOR  | Lista paginada (filtros: estado/cliente) |
| GET    | /api/mp/suscripciones/{id}                  | SISTEMA, ADMINISTRADOR  | Detalle con JOIN plan+cliente            |
| POST   | /api/mp/suscripciones/crear-con-redirect    | SISTEMA, ADMINISTRADOR  | Crea suscripción → devuelve init_point   |
| POST   | /api/mp/suscripciones/crear-con-token       | SISTEMA, ADMINISTRADOR  | Crea suscripción con card_token de Bricks|
| POST   | /api/mp/suscripciones/cancelar              | SISTEMA, ADMINISTRADOR  | Cancela en MP + localmente               |
| PUT    | /api/mp/suscripciones/actualizar-medio-pago | SISTEMA, ADMINISTRADOR  | Actualiza tarjeta en MP                  |
| POST   | /api/mp/webhooks                            | **AllowAnonymous**      | Recibe notificaciones de MP              |

---

## Flujo Opción B (Redirect) — paso a paso

```
1. Frontend → POST /api/mp/suscripciones/crear-con-redirect
2. Backend crea pre_approval en MP (status=pending, sin card_token)
3. Backend guarda suscripción local (estado=pending) y devuelve:
   { "initPoint": "https://www.mercadopago.com.ar/subscriptions/checkout?preapproval_id=..." }
4. Frontend redirige el navegador del cliente a initPoint
5. Cliente ingresa tarjeta en checkout de MP
6. MP notifica via POST /api/mp/webhooks (type=subscription_preapproval, data.id=...)
7. Backend actualiza estado → "authorized" y registra MpPayerId
8. MP notifica cada cobro via POST /api/mp/webhooks (type=payment, data.id=payment_id)
9. Backend registra MpTransaccion y actualiza ProximoCobro
```

---

## Dunning — escalación automática

El `DunningBackgroundService` corre cada 6 horas. Lógica:

| Días vencido | Acción                   | Estado suscripción |
|:------------:|--------------------------|-------------------|
| 1 – 3        | Email                    | sin cambio         |
| 3 – 7        | Email + WhatsApp         | sin cambio         |
| 7 – 9        | Email + WhatsApp + aviso | sin cambio         |
| 9 – 15       | Suspender en MP          | `suspended`        |
| 15+          | Cancelar en MP           | `cancelled`        |

Implementar `IDunningNotificador` en el sistema existente para enviar las notificaciones.

---

## Validación de firma webhook (HMAC-SHA256)

MP envía en el header `x-signature: ts=<timestamp>,v1=<hash>`.
El manifest que se firma es: `id:<data.id>;request-id:<x-request-id>;ts:<ts>;`
El secreto es `MercadoPago:WebhookSecret` de configuración.
