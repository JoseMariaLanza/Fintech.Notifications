using Fintech.Shared.Events;
using Fintech.Shared.Messaging;
using MediatR;
using Notifications.Application.Transfers.Commands.LogTransfer;

namespace Notifications.Consumer
{
    // Suscriptor: evento externo -> CQRS Command interno
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IEventBus _eventBus; // Singleton (adapter InMemory/Rabbit)
        private readonly IServiceScopeFactory _scopes; // Para crear un scope por evento

        public Worker(ILogger<Worker> logger, IEventBus eventBus, IServiceScopeFactory scopes)
        {
            _logger = logger;
            _eventBus = eventBus;
            _scopes = scopes;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notifications.Consumer started. Subscribing {Event}", nameof(TransferCompleted));
            _eventBus.Subscribe<TransferCompleted>(async evt =>
            {
                using var scope = _scopes.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                try
                {
                    _logger.LogDebug("Processing TransferCompleted {TransferId}", evt.TransferId);

                    await mediator.Send(new LogTransferCommand(
                        Type: "TransferCompleted",
                        Version: 1,
                        TransferId: evt.TransferId,
                        FromAccountId: evt.FromAccountId,
                        ToAccountId: evt.ToAccountId,
                        Amount: evt.Amount,
                        OccurredAtUtc: evt.OccurredAt.Kind == DateTimeKind.Utc
                            ? evt.OccurredAt
                            : evt.OccurredAt.ToUniversalTime()
                        ), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing TransferId={TransferId}", evt.TransferId);
                }
            });

            _logger.LogInformation("Registered Subscription. Waiting for events...");
            return Task.CompletedTask;

            //while (!stoppingToken.IsCancellationRequested)
            //{
            //    if (_logger.IsEnabled(LogLevel.Information))
            //    {
            //        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            //    }
            //    await Task.Delay(1000, stoppingToken);
            //}
        }
    }
}
