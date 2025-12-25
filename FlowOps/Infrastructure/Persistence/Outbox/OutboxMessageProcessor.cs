using FlowOps.BuildingBlocks.Integration;
using FlowOps.BuildingBlocks.Messaging;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FlowOps.Infrastructure.Persistence.Outbox
{
    public sealed class OutboxMessageProcessor : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxMessageProcessor> _logger;
        private readonly JsonSerializerOptions _serializerOptions = new(JsonSerializerDefaults.Web);
        private static readonly TimeSpan DelayBetweenBatches = TimeSpan.FromSeconds(2);

        public OutboxMessageProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxMessageProcessor> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessBatchAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while processing outbox messages.");
                }

                await Task.Delay(DelayBetweenBatches, stoppingToken).ConfigureAwait(false);
            }
        }

        private async Task ProcessBatchAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<FlowOpsDbContext>();
            var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

            var messages = await dbContext.OutboxMessages
                .Where(m => m.ProcessedAt == null)
                .OrderBy(m => m.OccurredOn)
                .Take(20)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(false);

            if (!messages.Any())
            {
                return;
            }

            foreach (var message in messages)
            {
                try
                {
                    var eventType = Type.GetType(message.Type);
                    if (eventType is null)
                    {
                        message.Error = $"Unknown event type: {message.Type}";
                        message.ProcessedAt = DateTime.UtcNow;
                        continue;
                    }

                    var integrationEvent = JsonSerializer.Deserialize(message.Payload, eventType, _serializerOptions) as IntegrationEvent;
                    if (integrationEvent is null)
                    {
                        message.Error = "Failed to deserialize integration event.";
                        message.ProcessedAt = DateTime.UtcNow;
                        continue;
                    }

                    await eventBus.PublishAsync(integrationEvent).ConfigureAwait(false);

                    message.ProcessedAt = DateTime.UtcNow;
                    message.Error = null;

                    _logger.LogInformation(
                        "Published outbox message {MessageId} for event {EventType}.",
                        message.Id,
                        message.Type);
                }
                catch (Exception ex)
                {
                    message.Error = ex.ToString();
                    message.ProcessedAt = DateTime.UtcNow;
                    _logger.LogError(ex, "Failed to publish outbox message {MessageId}.", message.Id);
                }
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}
