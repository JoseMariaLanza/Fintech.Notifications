using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Fintech.Shared.Events;
using Fintech.Shared.Messaging;
using FluentAssertions;

namespace Notifications.UnitTests.Common.Messaging
{
    public class EventBusSmokeTests
    {
        [Fact]
        public async Task Subscribe_then_publish_invokes_handler()
        {
            // Arrange
            var bus = new InMemoryEventBus();
            var called = false;

            bus.Subscribe<TransferCompleted>(evt =>
            {
                called = true;
                return Task.CompletedTask;
            });

            // Act
            await bus.PublishAsync(new TransferCompleted(
                TransferId: Guid.NewGuid(),
                FromAccountId: Guid.NewGuid(),
                ToAccountId: Guid.NewGuid(),
                Amount: 10m,
                OccurredAt: DateTime.UtcNow));

            // Assert
            called.Should().BeTrue();
        }
    }
}
