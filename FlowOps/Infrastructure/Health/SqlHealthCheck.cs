using System.Data;
using FlowOps.Infrastructure.Sql;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FlowOps.Infrastructure.Health
{
    public sealed class SqlHealthCheck : IHealthCheck
    {
        private readonly ISqlConnectionFactory _factory;
        public SqlHealthCheck(ISqlConnectionFactory factory)
        {
            _factory = factory;
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var connection = await _factory.CreateOpenAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandType = CommandType.Text;
                command.CommandText = "SELECT 1;";
                await command.ExecuteScalarAsync(cancellationToken);
                return HealthCheckResult.Healthy("SQL database is reachable.");
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
