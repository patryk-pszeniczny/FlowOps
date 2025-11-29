using FlowOps.Contracts.Response;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FlowOps.Infrastructure.Sql.Reporting
{
    public sealed class SqlReportingQueries : ISqlReportingQueries
    {
        private readonly ISqlConnectionFactory _connectionFactory;
        public SqlReportingQueries(ISqlConnectionFactory connectionFactory)
        {
            _connectionFactory = connectionFactory;
        }

        public async Task<CustomerReportSqlResponse?> GetCustomerReportAsync(Guid customerId, CancellationToken ct = default)
        {
            await using var connection = await _connectionFactory.CreateOpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = @"
                SELECT 
                    CustomerId,
                    ActiveSubscriptions,
                    TotalInvoiced,
                    TotalPaid
                FROM 
                    dbo.CustomerReports
                WHERE 
                    CustomerId = @cid";
            command.Parameters.Add(
                new SqlParameter("@cid", SqlDbType.UniqueIdentifier) 
                { 
                    Value = customerId 
                });

            await using var reader = await command.ExecuteReaderAsync(ct);
            if(!await reader.ReadAsync(ct))
            {
                return null;
            }
            return new CustomerReportSqlResponse(
                reader.GetGuid(0),
                reader.GetInt32(1),
                reader.GetDecimal(2),
                reader.GetDecimal(3)
            );
        }
        public async Task<IReadOnlyList<Guid>> GetActiveSubscriptionIdsAsync(Guid customerId, CancellationToken ct = default)
        {
            var result = new List<Guid>();
            await using var connection = await _connectionFactory.CreateOpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = @"
                SELECT 
                    SubscriptionId
                FROM 
                    dbo.ActiveSubscriptionIds
                WHERE 
                    CustomerId = @cid
                ORDER BY
                    SubscriptionId;";
            command.Parameters.Add(
                new SqlParameter("@cid", SqlDbType.UniqueIdentifier)
                {
                    Value = customerId
                });

            await using var reader = await command.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                result.Add(reader.GetGuid(0));
            }
            return result;
        }

        public async Task<IReadOnlyList<SubscriptionSqlResponse>> GetByCustomerAsync(Guid customerId, string? status = null, CancellationToken ct = default)
        {
            var list = new List<SubscriptionSqlResponse>();

            await using var connection = await _connectionFactory.CreateOpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;

            command.CommandText = @"
                SELECT 
                    SubscriptionId,
                    CustomerId,
                    PlanCode,
                    Status,
                    ActivatedAt,
                    SuspendedAt,
                    ResumedAt,
                    CancelledAt
                FROM 
                    dbo.Subscriptions
                WHERE 
                    CustomerId = @cid AND (@st IS NULL OR Status = @st)
                ORDER BY ActivatedAt DESC;";
            command.Parameters.Add(
                new SqlParameter("@cid", SqlDbType.UniqueIdentifier)
                {
                    Value = customerId
                });
            command.Parameters.Add(
                new SqlParameter("@st", SqlDbType.NVarChar, 16)
                {
                    Value = (object?)status ?? DBNull.Value
                });

            await using var reader = await command.ExecuteReaderAsync(ct);
            while(await reader.ReadAsync(ct))
            {
                list.Add(new SubscriptionSqlResponse
                (
                    reader.GetGuid(0), // SubscriptionId
                    reader.GetGuid(1), // CustomerId
                    reader.GetString(2), // PlanCode
                    reader.GetString(3), // Status
                    reader.GetDateTime(4), // ActivatedAt
                    reader.IsDBNull(5) ? null : reader.GetDateTime(5), // SuspendedAt
                    reader.IsDBNull(6) ? null : reader.GetDateTime(6), // ResumedAt
                    reader.IsDBNull(7) ? null : reader.GetDateTime(7) // CancelledAt
                ));
            }
            return list;
        }
        public async Task<SubscriptionSqlResponse?> GetSubscriptionByIdAsync(Guid subscriptionId, CancellationToken ct = default)
        {
            await using var connection = await _connectionFactory.CreateOpenAsync(ct);
            await using var command = connection.CreateCommand();
            command.CommandType = CommandType.Text;
            command.CommandText = @"
                SELECT 
                    SubscriptionId,
                    CustomerId,
                    PlanCode,
                    Status,
                    ActivatedAt,
                    SuspendedAt,
                    ResumedAt,
                    CancelledAt
                FROM 
                    dbo.Subscriptions
                WHERE 
                    SubscriptionId = @sid;";
            command.Parameters.Add(new SqlParameter("@sid", SqlDbType.UniqueIdentifier)
                {
                    Value = subscriptionId
                });
            await using var reader = await command.ExecuteReaderAsync(ct);
            if(!await reader.ReadAsync(ct))
            {
                return null;
            }
            return new SubscriptionSqlResponse
            (
                reader.GetGuid(0), // SubscriptionId
                reader.GetGuid(1), // CustomerId
                reader.GetString(2), // PlanCode
                reader.GetString(3), // Status
                reader.GetDateTime(4), // ActivatedAt
                reader.IsDBNull(5) ? null : reader.GetDateTime(5), // SuspendedAt
                reader.IsDBNull(6) ? null : reader.GetDateTime(6), // ResumedAt
                reader.IsDBNull(7) ? null : reader.GetDateTime(7) // CancelledAt
            );
        }
    }
}
