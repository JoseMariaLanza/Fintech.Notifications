# Fintech.Notifications

Notification and audit microservice that consumes RabbitMQ events published by other Fintech services and persists them as an audit trail in PostgreSQL.

## Architecture

Clean Architecture with CQRS via MediatR. Dependency flows inward:

```
Domain  <--  Application  <--  Infrastructure  <--  API / Consumer
```

```
Notifications.Domain/            # Pure entities, no framework deps
└── TransferNotification.cs       # Audit record entity

Notifications.Application/       # CQRS commands/handlers, abstractions
├── Transfers/Commands/
│   └── LogTransfer/              # LogTransferCommand + idempotent handler
└── INotificationsDbContext.cs    # Port for persistence

Notifications.Infrastructure/    # EF Core, entity configs, migrations
├── NotificationsDbContext.cs      # PostgreSQL context
├── Configurations/                # Entity type configurations
└── Migrations/                    # EF Core migrations

Notifications.API/               # ASP.NET Core Minimal API host
└── Program.cs                    # Health checks, Swagger, auto-migration

Notifications.Consumer/          # .NET Worker Service
├── RabbitMqTransferCompletedWorker.cs  # BackgroundService consumer
└── RabbitMqOptions.cs                   # Strongly-typed config

Notifications.UnitTests/         # xUnit unit tests
Notifications.IntegrationTests/  # Testcontainers + PostgreSQL
```

## Technology stack

| Component | Version |
|---|---|
| .NET | 8.0 |
| EF Core (PostgreSQL) | 9.0 |
| MediatR | 13.1 |
| RabbitMQ.Client | 7.2.0 |
| Fintech.Shared (NuGet) | 1.0.3 |
| xUnit | 2.5.3 |
| FluentAssertions | 8.8 |
| Testcontainers.PostgreSql | 4.8.1 |

## Commands

```bash
# Build
dotnet build Fintech.Notifications.sln

# Test
dotnet test Fintech.Notifications.sln

# Run API
dotnet run --project Notifications.API

# Run Consumer
dotnet run --project Notifications.Consumer

# Migrations
dotnet ef migrations add <Name> --project Notifications.Infrastructure --startup-project Notifications.API
dotnet ef database update --project Notifications.Infrastructure --startup-project Notifications.API
```

## RabbitMQ topology

| Setting | Value |
|---|---|
| Exchange | `fintech.events` (topic, durable) |
| Queue | `notifications.transfer.completed` (durable) |
| Routing key | `transfer.completed` |

The consumer uses `AsyncEventingBasicConsumer` with manual ACK/NACK.

## Database

- Engine: PostgreSQL
- Table: `notifications` with indexes on `transferid` and `type_version_when`
- `RawPayload`: stored as `jsonb` for event replay
- Auto-migration when `RunMigrationsOnStartup=true` (Development only)

## Key patterns

- **Idempotent handler**: checks `ExistsByTransferIdAsync` before insert (safe for at-least-once delivery)
- **JSONB payload storage**: full event serialized for audit replay
- **BackgroundService consumer**: manages RabbitMQ connection/channel lifecycle, idempotent exchange/queue/binding declaration

## Integration

```
Fintech.Accounts ──publishes──> TransferCompleted (RabbitMQ)
                                 └── consumed by: Fintech.Notifications

Fintech.Notifications ──uses──> Fintech.Shared (NuGet 1.0.3)
                                 └── TransferCompleted event contract

(Planned) OpsFlow ──publishes──> OperationalNotificationRequested
                                  └── will be consumed by: Fintech.Notifications
```
