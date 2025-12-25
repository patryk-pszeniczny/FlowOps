using FlowOps.Application.Common;
using FlowOps.BuildingBlocks.Diagnostics;
using FlowOps.BuildingBlocks.Integration;
using FlowOps.Infrastructure.Persistence;
using System.Text.Json;

namespace FlowOps.Infrastructure.Persistence.Outbox
{
    public sealed class OutboxMessageWriter : IOutboxMessageWriter
    {
        private readonly FlowOpsDbContext _dbContext;
        private readonly JsonSerializerOptions _serializerOptions;
        private readonly ILogger<OutboxMessageWriter> _logger;

        public OutboxMessageWriter(FlowOpsDbContext dbContext, ILogger<OutboxMessageWriter> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
            _serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        }

        public Task AddAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default)
        {
            if (integrationEvent is null)
            {
                throw new ArgumentNullException(nameof(integrationEvent));
            }

            var typeName = integrationEvent.GetType().AssemblyQualifiedName ?? integrationEvent.GetType().FullName!;
            var payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), _serializerOptions);

            var message = new OutboxMessage
            {
                Id = integrationEvent.Id,
                OccurredOn = integrationEvent.OccurredOn,
                Type = typeName,
                Payload = payload
            };

            _dbContext.OutboxMessages.Add(message);

            _logger.LogDebug(
                "Stored integration event {EventType} with Id={EventId} in outbox.",
                typeName,
                integrationEvent.Id);

            FlowOpsMetrics.OutboxMessagesProcessed.Add(1, KeyValuePair.Create<string, object?>("event", integrationEvent.GetType().Name));

            return Task.CompletedTask;
        }
    }
}