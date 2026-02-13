using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Notifications.Application.Abstractions;
using Notifications.Application.Transfers.Commands.LogTransfer;
using Notifications.Infrastructure.Persistence;
using Notifications.IntegrationTests.Infra;

namespace Notifications.IntegrationTests.Transfer
{
    public class LogTransferIntegrationTests : IClassFixture<PostgresNotificationsFixture>
    {
        private readonly PostgresNotificationsFixture _fixture;
        public LogTransferIntegrationTests(PostgresNotificationsFixture fixture) => _fixture = fixture;

        [Fact]
        public async Task Command_persists_row_in_postgres()
        {
            // 1) DbContext contra el Postgres efímero del fixture
            var opts = new DbContextOptionsBuilder<NotificationsDbContext>()
                .UseNpgsql(_fixture.Db.Database.GetConnectionString()).Options;

            await using var db = new NotificationsDbContext(opts);

            // 2) Instanciás el handler DIRECTO (como en el monolito)
            var handler = new LogTransferCommandHandler(db);

            // 3) Armás el comando
            var cmd = new LogTransferCommand(
                Type: "TransferCompleted",
                Version: 1,
                TransferId: Guid.NewGuid(),
                FromAccountId: Guid.NewGuid(),
                ToAccountId: Guid.NewGuid(),
                Amount: 99m,
                OccurredAtUtc: DateTime.UtcNow
            );

            // 4) Ejecutás el handler
            await handler.Handle(cmd, CancellationToken.None);

            // 5) Assert directo en la tabla (mismo DbContext)
            var count = await db.Notifications.CountAsync(n => n.TransferId == cmd.TransferId);
            count.Should().Be(1);
        }
    }
}
