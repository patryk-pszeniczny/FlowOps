using FlowOps.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;

namespace FlowOps.Infrastructure.Health
{
    public sealed class InboxHealthCheck : IHealthCheck
    {
        private readonly FlowOpsDbContext _dbContext;

        public InboxHealthCheck(FlowOpsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var processedLastHour = await _dbContext.InboxMessages
                    .Where(m => m.ProcessedAt > DateTime.UtcNow.AddHours(-1))
                    .CountAsync(cancellationToken)
                    .ConfigureAwait(false);

                var data = new Dictionary<string, object>
                {
                    ["processedLastHour"] = processedLastHour
                };
                return HealthCheckResult.Healthy("Inbox is reachable.", data);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Failed to query inbox state.", ex);
            }
        }
    }
}