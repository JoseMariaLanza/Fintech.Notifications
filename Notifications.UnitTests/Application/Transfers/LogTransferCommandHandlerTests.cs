using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Notifications.Application.Abstractions;
using Notifications.Application.Transfers.Commands.LogTransfer;
using Notifications.Domain.Notifications.Entities;

namespace Notifications.UnitTests.Application.Transfers
{
    // Fake minimal para probar la lógica de Application sin EF
    internal sealed class FakeNotificationsDbContext : INotificationsDbContext
    {
        public readonly List<TransferNotification> Store = new();

        public Task<bool> ExistsByTransferIdAsync(Guid transferId, CancellationToken ct = default)
            => Task.FromResult(Store.Any(n => n.TransferId == transferId));

        public Task AddAsync(TransferNotification notification, CancellationToken ct = default)
        { Store.Add(notification); return Task.CompletedTask; }

        public Task<int> SaveChangesAsync(CancellationToken ct = default) => Task.FromResult(1);
    }

    public class LogTransferCommandHandlerTests
    {
        [Fact]
        public async Task Handle_should_persist_only_once_by_transferId()
        {
            // Arrange
            var db = new FakeNotificationsDbContext();
            var sut = new LogTransferCommandHandler(db);

            var now = DateTime.UtcNow;
            var cmd = new LogTransferCommand(
                Type: "TransferCompleted",
                Version: 1,
                TransferId: Guid.NewGuid(),
                FromAccountId: Guid.NewGuid(),
                ToAccountId: Guid.NewGuid(),
                Amount: 25m,
                OccurredAtUtc: now);

            // Act
            await sut.Handle(cmd, default);
            await sut.Handle(cmd, default);

            // Assert
            db.Store.Count.Should().Be(1);
            db.Store.Single().OccurredAtUtc.Should().BeCloseTo(now, TimeSpan.FromSeconds(1));
        }
    }
}
