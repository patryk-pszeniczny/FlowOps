using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using FlowOps.BuildingBlocks.Integration;
using FlowOps.BuildingBlocks.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace FlowOps.Infrastructure.Messaging
{
    public sealed class RabbitMqEventBus : IEventBus, IAsyncDisposable
    {
        private readonly ConnectionFactory _factory;
        private readonly ILogger<RabbitMqEventBus> _logger;
        private readonly JsonSerializerOptions _serializerOptions;

        private const string ExchangeName = "flowops.integration";

        private IConnection? _connection;
        private IChannel? _channel;

        private readonly SemaphoreSlim _connectLock = new(1, 1);
        private readonly ConcurrentDictionary<string, byte> _subscriptions = new();

        public RabbitMqEventBus(
            IConfiguration configuration,
            ILogger<RabbitMqEventBus> logger)
        {
            _logger = logger;

            var hostName = configuration["RabbitMQ:HostName"] ?? "localhost";
            var portString = configuration["RabbitMQ:Port"];
            var userName = configuration["RabbitMQ:UserName"] ?? "guest";
            var password = configuration["RabbitMQ:Password"] ?? "guest";
            var virtualHost = configuration["RabbitMQ:VirtualHost"] ?? "/";

            var port = string.IsNullOrWhiteSpace(portString)
                ? 5672 : int.Parse(portString);

            _factory = new ConnectionFactory
            {
                HostName = hostName,
                Port = port,
                UserName = userName,
                Password = password,
                VirtualHost = virtualHost,

                AutomaticRecoveryEnabled = true,

                ConsumerDispatchConcurrency = 1,

                ContinuationTimeout = TimeSpan.FromSeconds(30),
                HandshakeContinuationTimeout = TimeSpan.FromSeconds(30),

                RequestedConnectionTimeout = TimeSpan.FromSeconds(15),
            };


            _serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        }

        private async Task EnsureConnectedAsync(CancellationToken cancellationToken)
        {
            if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
            {
                return;
            }
            await _connectLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
                {
                    return;
                }
                _logger.LogInformation(
                    "RabbitMQ: connecting to {Host}:{Port}...",
                    _factory.HostName,
                    _factory.Port);

                _connection = await _factory
                    .CreateConnectionAsync(cancellationToken)
                    .ConfigureAwait(false);

                _channel = await _connection
                    .CreateChannelAsync(options: null, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                await _channel.ExchangeDeclareAsync(
                    exchange: ExchangeName,
                    type: "topic",
                    durable: true,
                    autoDelete: false,
                    arguments: null,
                    passive: false,
                    noWait: false,
                    cancellationToken: cancellationToken).ConfigureAwait(false);

                _logger.LogInformation(
                    "RabbitMQ: connected and using exchange '{Exchange}'.",
                    ExchangeName);
            }
            finally
            {
                _connectLock.Release();
            }
        }

        public async Task PublishAsync<T>(T @event) where T : IntegrationEvent
        {
            if (@event is null) throw new ArgumentNullException(nameof(@event));

            var ct = CancellationToken.None;
            await EnsureConnectedAsync(ct).ConfigureAwait(false);

            var eventType = @event.GetType();
            var routingKey = eventType.FullName ?? eventType.Name;

            var body = JsonSerializer.SerializeToUtf8Bytes(@event, eventType, _serializerOptions);

            var props = new BasicProperties
            {
                ContentType = "application/json",
                Type = eventType.AssemblyQualifiedName,
            };

            _logger.LogInformation(
                "RabbitMQ: publishing event {EventType} (Id={EventId}) to exchange '{Exchange}', rk='{RoutingKey}'.",
                routingKey,
                @event.Id,
                ExchangeName,
                routingKey);

            await _channel!.BasicPublishAsync(
                exchange: ExchangeName,
                routingKey: routingKey,
                mandatory: false,
                basicProperties: props,
                body: body,
                cancellationToken: ct).ConfigureAwait(false);
        }

        public void Subscribe<T>(Func<T, Task> handler) where T : IntegrationEvent
        {
            if (handler is null) throw new ArgumentNullException(nameof(handler));

            var eventType = typeof(T);
            var routingKey = eventType.FullName ?? eventType.Name;

            var subscriberName = handler.Target?.GetType().Name ?? "anonymous";
            var queueName = $"flowops.{subscriberName}.{eventType.Name}".ToLowerInvariant();

            var subscriptionKey = $"{queueName}|{routingKey}";
            if(!_subscriptions.TryAdd(subscriptionKey, 0))
            {
                _logger.LogInformation(
                      "RabbitMQ: already subscribed. queue='{Queue}', rk='{RoutingKey}'.",
                      queueName,
                      routingKey);
                return;
            }

            _ = Task.Run(async () =>
            {
                var ct = CancellationToken.None;

                try
                {
                    await EnsureConnectedAsync(ct).ConfigureAwait(false);

                    await _channel!.QueueDeclareAsync(
                        queue: queueName,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null,
                        passive: false,
                        noWait: false,
                        cancellationToken: ct).ConfigureAwait(false);

                    await _channel.QueueBindAsync(
                        queue: queueName,
                        exchange: ExchangeName,
                        routingKey: routingKey,
                        arguments: null,
                        noWait: false,
                        cancellationToken: ct).ConfigureAwait(false);

                    var consumer = new AsyncEventingBasicConsumer(_channel);

                    consumer.ReceivedAsync += async (sender, ea) =>
                    {
                        try
                        {
                            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                            var message = JsonSerializer.Deserialize<T>(json, _serializerOptions);

                            if (message != null)
                            {
                                await handler(message).ConfigureAwait(false);
                            }
                            await _channel.BasicAckAsync(
                                deliveryTag: ea.DeliveryTag,
                                multiple: false).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                ex,
                                "RabbitMQ: error while message of type {EventType}.",
                                eventType.Name);

                            await _channel.BasicNackAsync(
                                deliveryTag: ea.DeliveryTag,
                                multiple: false,
                                requeue: true).ConfigureAwait(false);
                        }
                    };

                    await _channel.BasicConsumeAsync(
                        queue: queueName,
                        autoAck: false,
                        consumer: consumer,
                        cancellationToken: ct).ConfigureAwait(false);

                    _logger.LogInformation(
                        "RabbitMQ: subscribed. subscriber='{Subscriber}', event='{Event}', queue='{Queue}', rk='{RoutingKey}'.",
                        subscriberName,
                        eventType.Name,
                        queueName,
                        routingKey);

                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "RabbitMQ: failed to subscribe to {EventType}.",
                        eventType.Name);
                }
            });
        }

        public async ValueTask DisposeAsync()
        {
            if (_channel is not null)
            {
                try
                {
                    await _channel.CloseAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "RabbitMQ: error while closing channel.");
                }
            }

            if (_connection is not null)
            {
                try
                {
                    await _connection.CloseAsync().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "RabbitMQ: error while closing connection.");
                }
            }
        }
    }
}
