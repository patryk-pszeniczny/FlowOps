using FlowOps.Application.Common;
using FlowOps.BuildingBlocks.Integration;
using Microsoft.EntityFrameworkCore;

namespace FlowOps.Infrastructure.Persistence.Inbox
{
    public sealed class IntegrationEventInBox : IIntegrationEventInbox
    {
        private readonly FlowOpsDbContext _dbContext;
        private readonly ILogger<IntegrationEventInBox> _logger;
        public IntegrationEventInBox(FlowOpsDbContext dbContext, ILogger<IntegrationEventInBox> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }
        public async Task<bool> HasProcessedAsync(string consumer, Guid eventId, CancellationToken cancellationToken = default)
        {
            return await _dbContext.InboxMessages.AnyAsync(
                x => x.Consumer == consumer && x.EventId == eventId,
                cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task MarkProcessedAsync(string consumer, Guid eventId, CancellationToken cancellationToken = default)
        {
            _dbContext.InboxMessages.Add(new InboxMessage
            {
                Consumer = consumer,
                EventId = eventId,
                ProcessedAt = DateTime.UtcNow
            });
            await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task ProcessAsync<TEvent>(string conusmer, TEvent evt, Func<CancellationToken, Task> handler, CancellationToken cancellationToken = default) where TEvent : IntegrationEvent
        {
            if(await HasProcessedAsync(conusmer, evt.Id, cancellationToken).ConfigureAwait(false))
            {
                _logger.LogInformation("Event {EventId} has already been processed by consumer {Consumer}. Skipping.", evt.Id, conusmer);
                return;
            }
            await handler(cancellationToken).ConfigureAwait(false);
            await MarkProcessedAsync(conusmer, evt.Id, cancellationToken).ConfigureAwait(false);
        }
    }
}
