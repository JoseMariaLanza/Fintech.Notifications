//using Notifications.Consumer;

//var builder = Host.CreateApplicationBuilder(args);

//builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));

//builder.Services.AddHostedService<RabbitMqTransferCompletedWorker>();
////builder.Services.AddHostedService<Worker>();

//var host = builder.Build();
//host.Run();

// Notifications.Consumer/Program.cs
using MediatR;
using Microsoft.EntityFrameworkCore;
using Notifications.Application.Abstractions;
using Notifications.Application.Transfers.Commands.LogTransfer;
using Notifications.Consumer;
using Notifications.Infrastructure.Persistence;

var builder = Host.CreateApplicationBuilder(args);

// *** CAMBIO: opciones de RabbitMQ (asegurate que la sección se llame "RabbitMq") ***
builder.Services.Configure<RabbitMqOptions>(builder.Configuration.GetSection("RabbitMq"));

// *** CAMBIO: DbContext + puerto Application ***
builder.Services.AddDbContext<NotificationsDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Fintech")));

builder.Services.AddScoped<INotificationsDbContext>(sp =>
    sp.GetRequiredService<NotificationsDbContext>());

// *** CAMBIO: MediatR para LogTransferCommand ***
builder.Services.AddMediatR(m =>
    m.RegisterServicesFromAssemblyContaining<LogTransferCommand>());

// Worker nuevo basado en RabbitMQ.Client 7.2
builder.Services.AddHostedService<RabbitMqTransferCompletedWorker>();

var host = builder.Build();
host.Run();
