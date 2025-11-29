using FlowOps.Contracts.Response;
using FlowOps.Contracts.Result;
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
        public async Task<PagedResult<SubscriptionSqlResponse>> GetByCustomerPagedAsync(
            Guid customerId,
            int page,
            int pageSize,
            string? orderBy = null,
            string? orderDirection = null,
            string? status = null,
            CancellationToken ct = default)
        {
            page = Math.Max(page, 1); // (1-based indexing)
            pageSize = Math.Clamp(pageSize, 1, 200); // <1 to 200>

            var orderColumn = (orderBy?.Trim()) switch
            {
                "Status" => "Status",
                "ActivatedAt" or null or "" => "ActivatedAt",
                _ => "ActivatedAt"
            };
            var orderDir = (orderDirection?.Trim().ToUpperInvariant()) switch
            {
                "ASC" => "ASC",
                "DESC" => "DESC",
                _ => "DESC"
            };
            var skip = (page - 1) * pageSize;

            int totalCount;
            await using (var connection = await _connectionFactory.CreateOpenAsync(ct))
            {
                await using (var countCommand = connection.CreateCommand())
                {
                    countCommand.CommandType = CommandType.Text;
                    countCommand.CommandText = @"
                        SELECT 
                            COUNT(*) 
                        FROM 
                            dbo.Subscriptions
                        WHERE 
                            CustomerId = @cid AND (@st IS NULL OR Status = @st);";
                    countCommand.Parameters.Add(
                        new SqlParameter("@cid", SqlDbType.UniqueIdentifier)
                        {
                            Value = customerId
                        });
                    countCommand.Parameters.Add(
                        new SqlParameter("@st", SqlDbType.NVarChar, 16)
                        {
                            Value = (object?)status ?? DBNull.Value
                        });
                    totalCount = (int)(await countCommand.ExecuteScalarAsync(ct) ?? 0);
                }
            }
            var items = new List<SubscriptionSqlResponse>();
            await using (var connection = await _connectionFactory.CreateOpenAsync(ct))
            {
                await using (var command = connection.CreateCommand())
                {
                    command.CommandType = CommandType.Text;
                    command.CommandText = $@"
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
                            CustomerId = @cid AND (@st IS NULL or Status = @st)
                        ORDER BY
                            @order @direction, SubscriptionId ASC
                        OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;";
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
                    command.Parameters.Add(
                        new SqlParameter("@skip", SqlDbType.Int)
                        {
                            Value = skip
                        });
                    command.Parameters.Add(
                        new SqlParameter("@take", SqlDbType.Int)
                        {
                            Value = pageSize
                        });
                    command.Parameters.Add(
                        new SqlParameter("@order", SqlDbType.NVarChar, 32)
                        {
                            Value = orderColumn
                        });
                    command.Parameters.Add(
                        new SqlParameter("@direction", SqlDbType.NVarChar, 4)
                        {
                            Value = orderDir
                        });
                    await using var reader = await command.ExecuteReaderAsync(ct);
                    while(await reader.ReadAsync(ct))
                    {
                        items.Add(new SubscriptionSqlResponse
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
                }
            }

            var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
            return new PagedResult<SubscriptionSqlResponse>(
                Items: items,
                Page: page,
                PageSize: pageSize,
                TotalCount: totalCount,
                TotalPages: totalPages
            );
        }
    }
}
