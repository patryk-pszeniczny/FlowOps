using FlowOps.Contracts.Response;

namespace FlowOps.Infrastructure.Sql.Reporting
{
    public interface ISqlReportingQueries
    {
        Task<CustomerReportSqlResponse?> GetCustomerReportAsync(Guid customerId, CancellationToken ct = default);
        Task<IReadOnlyList<Guid>> GetActiveSubscriptionIdsAsync(Guid customerId, CancellationToken ct = default);
        Task<IReadOnlyList<SubscriptionSqlResponse>> GetByCustomerAsync(Guid customerId, string? status = null, CancellationToken ct = default);
        Task<SubscriptionSqlResponse?> GetSubscriptionByIdAsync(Guid subscriptionId, CancellationToken ct = default);
    }
}
