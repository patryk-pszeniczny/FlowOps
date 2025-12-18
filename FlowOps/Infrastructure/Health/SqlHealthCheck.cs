using System.Data;
using FlowOps.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FlowOps.Infrastructure.Health
{
    public sealed class SqlHealthCheck : IHealthCheck
    {
        private readonly FlowOpsDbContext _dbContex;
        public SqlHealthCheck(FlowOpsDbContext dbContext)
        {
            _dbContex = dbContext;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                if(await _dbContex.Database.CanConnectAsync(cancellationToken))
                {
                    return HealthCheckResult.Healthy("SQL database is reachable.");
                }
                return HealthCheckResult.Unhealthy("SQL databaase is not reachable.");
            }
            catch(SqlException ex)
            {
                return HealthCheckResult.Unhealthy("SQL database is not reachable.", ex);
            }
            catch(Exception ex)
            {
                return HealthCheckResult.Unhealthy("An unexpected error occurred while checking SQL database health.", ex);
            }
        }
    }
}
