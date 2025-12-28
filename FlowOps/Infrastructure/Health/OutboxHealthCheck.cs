using FlowOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;

namespace FlowOps.Infrastructure.Health
{
    public sealed class OutboxHealthCheck : IHealthCheck
    {
        private readonly FlowOpsDbContext _dbContext;

        public OutboxHealthCheck(FlowOpsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var pendingCount = await _dbContext.OutboxMessages
                    .Where(m => m.ProcessedAt == null)
                    .CountAsync(cancellationToken)
                    .ConfigureAwait(false);

                var oldestPending = await _dbContext.OutboxMessages
                    .Where(m => m.ProcessedAt == null)
                    .OrderBy(m => m.OccurredOn)
                    .Select(m => (DateTime?)m.OccurredOn)
                    .FirstOrDefaultAsync(cancellationToken)
                    .ConfigureAwait(false);

                var description = oldestPending == default
                    ? "Outbox is empty."
                    : $"Oldest pending message queued at {oldestPending:O}.";
                var data = new Dictionary<string, object>
                {
                    ["pendingCount"] = pendingCount
                };
                if(oldestPending is not null)
                {
                    data["oldestPending"] = oldestPending;
                }
                return HealthCheckResult.Healthy(description, data);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Failed to query outbox state.", ex);
            }
        }
    }
}