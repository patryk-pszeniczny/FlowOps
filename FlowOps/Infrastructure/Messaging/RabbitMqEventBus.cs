using FlowOps.BuildingBlocks.Integration;
using FlowOps.BuildingBlocks.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Channels;

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

        private readonly SemaphoreSlim _connectGate = new(1, 1);
        private const ushort PrefetchCount = 16;


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

        private async Task EnsureConnectedAsync(CancellationToken ct)
        {
            if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
                return;

            await _connectGate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                if (_connection is { IsOpen: true } && _channel is { IsOpen: true })
                    return;

                if (_channel is not null)
                {
                    try { await _channel.CloseAsync(replyCode: 200, replyText: "Reconnecting", abort: false, cancellationToken: CancellationToken.None).ConfigureAwait(false); }
                    catch { }
                    await _channel.DisposeAsync().ConfigureAwait(false);
                    _channel = null;
                }

                if (_connection is not null)
                {
                    try { await _connection.CloseAsync(reasonCode: 200, reasonText: "Reconnecting", timeout: TimeSpan.FromSeconds(5), abort: false, cancellationToken: CancellationToken.None).ConfigureAwait(false); }
                    catch { }
                    await _connection.DisposeAsync().ConfigureAwait(false);
                    _connection = null;
                }

                _logger.LogInformation("RabbitMQ: connecting to {Host}:{Port}...", _factory.HostName, _factory.Port);

                _connection = await _factory.CreateConnectionAsync(ct).ConfigureAwait(false);
                _channel = await _connection.CreateChannelAsync(options: null, cancellationToken: ct).ConfigureAwait(false);

                await _channel.ExchangeDeclareAsync(
                    exchange: ExchangeName,
                    type: "topic",
                    durable: true,
                    autoDelete: false,
                    arguments: null,
                    passive: false,
                    noWait: false,
                    cancellationToken: ct).ConfigureAwait(false);

                await _channel.BasicQosAsync(
                    prefetchSize: 0,
                    prefetchCount: PrefetchCount,
                    global: false,
                    cancellationToken: ct).ConfigureAwait(false);

                _logger.LogInformation("RabbitMQ: connected and using exchange '{Exchange}'.", ExchangeName);
            }
            finally
            {
                _connectGate.Release();
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

            var subscriber = ResolveSubscriberName(handler);
            var eventType = typeof(T);
            var routingKey = eventType.FullName ?? eventType.Name;
            var queueName = $"flowops.{subscriber}.{eventType.Name}".ToLowerInvariant();

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

                            if (message is not null)
                                await handler(message).ConfigureAwait(false);

                            await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "RabbitMQ: error while handling message of type {EventType}.", eventType.Name);
                            await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: true).ConfigureAwait(false);
                        }
                    };

                    await _channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer).ConfigureAwait(false);

                    _logger.LogInformation(
                        "RabbitMQ: subscribed. subscriber='{Subscriber}', event='{Event}', queue='{Queue}', rk='{RoutingKey}'.",
                        subscriber,
                        eventType.Name,
                        queueName,
                        routingKey);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "RabbitMQ: failed to subscribe to {EventType}.", eventType.Name);
                }
            });
    }

    private static string ResolveSubscriberName(Delegate handler)
    {
        var t = handler.Method.DeclaringType ?? handler.Target?.GetType();
        if (t is null) return "unknown";

        while (t.IsNested && (t.Name.Contains("DisplayClass") || t.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)))
        {
            t = t.DeclaringType ?? t;
            if (t.DeclaringType is null) break;
        }

        return Sanitize(t.Name);
    }

    private static readonly Regex _invalid = new(@"[^a-zA-Z0-9\-_.]+", RegexOptions.Compiled);

    private static string Sanitize(string value)
    {
        value = _invalid.Replace(value, "");
        return string.IsNullOrWhiteSpace(value) ? "unknown" : value;
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
