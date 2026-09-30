using System.Data;
using System.Text;
using BusinessLogic;
using DataAccess;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using UnitOfWork;
using WebApp.Extensions;
using WebApp.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------------------
// Base de datos — la conexión se crea una sola vez (Singleton UoW)
// Dapper abre/cierra conexiones por query usando el pool de SqlClient
// ----------------------------------------------------------------
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection no configurado.");

builder.Services.AddScoped<IUnitOfWork>(_ =>
    new DataAccessUnitOfWork(new SqlConnection(connectionString)));

// ----------------------------------------------------------------
// Auth standalone
// ----------------------------------------------------------------
builder.Services.AddScoped<BusinessLogic.IAuthLogic, BusinessLogic.AuthLogic>();

// ----------------------------------------------------------------
// Módulo Mercado Pago (lógicas + validators + DunningBackgroundService)
// ----------------------------------------------------------------
builder.Services.AddMercadoPagoServices(builder.Configuration);

// Notificador de dunning — reemplazar con la implementación real del sistema
builder.Services.AddScoped<IDunningNotificador, StubDunningNotificador>();

// ----------------------------------------------------------------
// FluentValidation automático
// ----------------------------------------------------------------
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// ----------------------------------------------------------------
// JWT Bearer
// ----------------------------------------------------------------
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key no configurado.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer    = true,
            ValidIssuer       = builder.Configuration["Jwt:Issuer"],
            ValidateAudience  = true,
            ValidAudience     = builder.Configuration["Jwt:Audience"],
            ValidateLifetime  = true,
            ClockSkew         = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ----------------------------------------------------------------
// Swagger con Bearer auth
// ----------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Cobros Recurrentes API",
        Version     = "v1",
        Description = "API de cobros recurrentes integrada con Mercado Pago"
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.ApiKey,
        Scheme       = "Bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Ingresar: Bearer {token}"
    };
    options.AddSecurityDefinition("Bearer", securityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// ----------------------------------------------------------------
// CORS
// ----------------------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>();
        if (origins?.Length > 0)
            policy.WithOrigins(origins).AllowAnyMethod().AllowAnyHeader();
        else
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
    });
});

builder.Services.AddControllers();

// ----------------------------------------------------------------
// AWS Lambda Hosting (ignorado en local, activo en Lambda)
// ----------------------------------------------------------------
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);

// ================================================================
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Cobros Recurrentes v1"));
}

app.UseCors();
app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
