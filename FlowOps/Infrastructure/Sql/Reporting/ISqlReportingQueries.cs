using FlowOps.Contracts.Response;

namespace FlowOps.Infrastructure.Sql.Reporting
{
    public interface ISqlReportingQueries
    {
        Task<CustomerReportSqlResponse?> GetCustomerReportAsync(Guid customerId, CancellationToken ct = default);
    }
}
