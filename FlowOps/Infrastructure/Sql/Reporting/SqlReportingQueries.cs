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
                    CustomerId = @CustomerId";
            command.Parameters.Add(
                new SqlParameter("@CustomerId", SqlDbType.UniqueIdentifier) 
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
    }
}
