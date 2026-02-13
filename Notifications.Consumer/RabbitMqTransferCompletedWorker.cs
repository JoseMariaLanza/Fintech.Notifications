using System.Text;
using System.Text.Json;
using Fintech.Shared.Events;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notifications.Application.Transfers.Commands.LogTransfer;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Notifications.Consumer
{
    public sealed class RabbitMqTransferCompletedWorker : BackgroundService
    {
        private readonly ILogger<RabbitMqTransferCompletedWorker> _logger;
        private readonly IServiceScopeFactory _scopes;
        private readonly RabbitMqOptions _options;

        private IConnection? _connection;   // v7: IConnection
        private IChannel? _channel;         // v7: IChannel (no IModel)

        public RabbitMqTransferCompletedWorker(
            ILogger<RabbitMqTransferCompletedWorker> logger,
            IServiceScopeFactory scopes,
            IOptions<RabbitMqOptions> options)
        {
            _logger = logger;
            _scopes = scopes;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // *** CREA CONEXIÓN + CANAL + TOPOLOGÍA ***
            var factory = new ConnectionFactory
            {
                HostName = _options.Host,
                UserName = _options.User,
                Password = _options.Pass,
                VirtualHost = _options.VHost
                // NOTA: en 7.x ya NO existe DispatchConsumersAsync
            };

            // API RabbitMQ.Client 7.x (async)
            _connection = await factory.CreateConnectionAsync(stoppingToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            // 1) Exchange (idempotente)
            await _channel.ExchangeDeclareAsync(
                exchange: _options.ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken);

            // 2) Queue (idempotente)
            await _channel.QueueDeclareAsync(
                queue: _options.QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: stoppingToken);

            // 3) Binding (idempotente)
            await _channel.QueueBindAsync(
                queue: _options.QueueName,
                exchange: _options.ExchangeName,
                routingKey: _options.RoutingKey,
                arguments: null,
                cancellationToken: stoppingToken);

            _logger.LogInformation(
                "RabbitMqTransferCompletedWorker listening on queue '{Queue}' (exchange '{Exchange}', routingKey '{RoutingKey}')",
                _options.QueueName, _options.ExchangeName, _options.RoutingKey);

            // 4) Consumer async (nota: en 7.2 el evento es ReceivedAsync)
            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += OnMessageReceivedAsync;

            await _channel.BasicConsumeAsync(
                queue: _options.QueueName,
                autoAck: false,
                consumer: consumer);

            // No hacemos loop manual; el canal + consumer quedan vivos
            // hasta que se cancele el token o se pare el host.
        }

        private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
        {
            if (_channel is null)
            {
                // Algo muy raro, pero evitamos NullReference
                return;
            }

            using var scope = _scopes.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var evt = JsonSerializer.Deserialize<TransferCompleted>(json);

                if (evt is null)
                {
                    _logger.LogError("Could not deserialize TransferCompleted message, DeliveryTag={DeliveryTag}", ea.DeliveryTag);

                    // v7: usamos BasicNackAsync
                    await _channel.BasicNackAsync(
                        deliveryTag: ea.DeliveryTag,
                        multiple: false,
                        requeue: false);

                    return;
                }

                _logger.LogInformation("Processing TransferCompleted {TransferId}", evt.TransferId);

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
                ), ea.CancellationToken);

                // Todo OK → ACK (también async)
                await _channel.BasicAckAsync(
                    deliveryTag: ea.DeliveryTag,
                    multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing TransferCompleted message, DeliveryTag={DeliveryTag}",
                    ea.DeliveryTag);

                // Error → NACK sin requeue (o requeue:true si querés reintentar)
                await _channel.BasicNackAsync(
                    deliveryTag: ea.DeliveryTag,
                    multiple: false,
                    requeue: false);
            }
        }

        public override void Dispose()
        {
            //_channel?.Close();
            _channel?.Dispose();
            //_connection?.Close();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
