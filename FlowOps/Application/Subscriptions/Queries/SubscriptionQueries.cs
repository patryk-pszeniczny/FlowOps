using FlowOps.Application.Reporting;
using FlowOps.Contracts.Item;
using FlowOps.Contracts.Response;
using FlowOps.Contracts.Result;
using FlowOps.Domain.Subscriptions;
using FlowOps.Reports.Stores;

namespace FlowOps.Application.Subscriptions.Queries
{
    public sealed class SubscriptionQueries
    {
        private readonly ISubscriptionRepository _repository;
        private readonly IReportingQueries _reportingQueries;
        public SubscriptionQueries(
            ISubscriptionRepository repository,
            IReportingQueries reportingQueries)
        {
            _repository = repository;
            _reportingQueries = reportingQueries;
        }
        public async Task<SubscriptionDetailsResponse> GetDetailsAsync(Guid id, CancellationToken ct = default)
        {
            var subscription = await _repository.GetByIdAsync(id, asNoTracking: true, ct: ct)
                ?? throw new KeyNotFoundException($"Subscription with ID '{id}' not found.");

            return new SubscriptionDetailsResponse(
                id: subscription.Id,
                CustomerId: subscription.CustomerId,
                PlanCode: subscription.PlanCode,
                Status: subscription.Status.ToString()
            );
        }
        public async Task<IReadOnlyList<SubscriptionListItem>> GetByCustomerAsync(Guid customerId, CancellationToken ct = default)
        {
            var subscriptions = await _repository.GetByCustomerAsync(customerId, ct);
            return subscriptions
                .Select(s => new SubscriptionListItem(
                    s.Id,
                    s.PlanCode,
                    s.Status.ToString()))
                .OrderBy(x => x.PlanCode)
                .ToList();
        }
        public Task<IReadOnlyList<SubscriptionSqlResponse>> GetByCustomerAsync(Guid customerId, string? status, CancellationToken ct = default) =>
            _reportingQueries.GetByCustomerAsync(customerId, status, ct);

        public Task<SubscriptionSqlResponse?> GetByIdAsync(Guid subscriptionId, CancellationToken ct = default) =>
            _reportingQueries.GetSubscriptionByIdAsync(subscriptionId, ct);

        public Task<PagedResult<SubscriptionSqlResponse>> GetByCustomerPagedAsync(
            Guid customerId,
            int page,
            int pageSize,
            string? orderBy,
            string? orderDirection,
            string? status,
            CancellationToken ct = default) =>
            _reportingQueries.GetByCustomerPagedAsync(customerId, page, pageSize, orderBy, orderDirection, status, ct);

        public Task<SubscriptionStatusSummaryResponse> GetStatusSummarySqlAsync(Guid customerId, CancellationToken ct = default) =>
            _reportingQueries.GetStatusSummaryAsync(customerId, ct);

    }
}
