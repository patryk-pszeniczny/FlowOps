using Microsoft.Data.SqlClient;

namespace FlowOps.Infrastructure.Sql
{
    public interface ISqlConnectionFactory
    {
        SqlConnection Create();
        Task<SqlConnection> CreateOpenAsync(CancellationToken ct = default);
    }
}
