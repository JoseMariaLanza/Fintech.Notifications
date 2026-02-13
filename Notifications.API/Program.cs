using MediatR;                                      // Descubre handlers en Notifications.Application
using Microsoft.EntityFrameworkCore;                // EF Core
using Notifications.Application.Abstractions;       // Puerto de Application (INotificationsDbContext)
using Notifications.Application.Transfers.Commands.LogTransfer;
using Notifications.Infrastructure.Persistence;     // DbContext de Infra

var builder = WebApplication.CreateBuilder(args);
var cfg = builder.Configuration;
var env = builder.Environment;

// --- Infra base (Swagger / endpoints) ---
builder.Services.AddEndpointsApiExplorer();         // Descubre endpoints mínimos para Swagger
builder.Services.AddSwaggerGen();                   // UI/Docs en Development

// --- HealthChecks (DB) ---
// Requiere paquete: Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore
builder.Services.AddHealthChecks()
    .AddDbContextCheck<NotificationsDbContext>("NotificationsDb"); // Chequea que el DbContext pueda conectarse

// --- EF Core + Npgsql (DbContext) ---
builder.Services.AddDbContext<NotificationsDbContext>(opt =>
    opt.UseNpgsql(cfg.GetConnectionString("Fintech")));      // Lee la CS "NotificationsDb" (appsettings*.json o env)

// --- Puerto Application -> DbContext Infra ---
// La capa Application depende de una abstracción (INotificationsDbContext). La implementa nuestro DbContext de Infra.
builder.Services.AddScoped<INotificationsDbContext>(sp =>
    sp.GetRequiredService<NotificationsDbContext>());

// --- MediatR (CQRS) ---
// Registra handlers/commands/behaviors ubicados en Notifications.Application
builder.Services.AddMediatR(m =>
    m.RegisterServicesFromAssemblyContaining<LogTransferCommand>());

var app = builder.Build();

// --- Migraciones automáticas SOLO en Dev y con flag ---
if (app.Environment.IsDevelopment() &&
    app.Configuration.GetValue<bool>("RunMigrationsOnStartup"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
    db.Database.Migrate();
}

// --- Pipeline mínimo + Swagger en Development ---
if (env.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// --- Endpoints de salud ---
// Opción A (endpoint simple, útil para compose): 200 OK si el proceso está vivo
app.MapGet("/health", () => Results.Ok("OK"));
// Opción B (healthchecks formales con resultados detallados):
// app.MapHealthChecks("/healthz");

app.Run();
