using Microsoft.Data.SqlClient;

namespace FlowOps.Infrastructure.Sql
{
    public sealed class SqlConnectionFactory : ISqlConnectionFactory
    {
        private readonly string _connectionString;
        public SqlConnectionFactory(IConfiguration configuration)
        {
            _connectionString =
                configuration.GetConnectionString("ReportingDb")
                ?? configuration["ConnectionStrings:ReportingDb"]
                ?? configuration["ConnectionStrings__ReportingDb"]
                ?? throw new InvalidOperationException(
                    "Missing connection string 'ReportingDb'. Set it via appsettings or env: ConnectionStrings__ReportingDb.");
        }
        public SqlConnection Create()
        {
            return new SqlConnection(_connectionString);
        }

        public async Task<SqlConnection> CreateOpenAsync(CancellationToken ct = default)
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(ct);
            return connection;
        }
    }
}
