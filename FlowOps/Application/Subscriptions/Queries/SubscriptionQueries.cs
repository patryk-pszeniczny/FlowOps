using FlowOps.Application.Reporting;
using FlowOps.Contracts.Item;
using FlowOps.Contracts.Response;
using FlowOps.Domain.Subscriptions;
using FlowOps.Reports.Stores;

namespace FlowOps.Application.Subscriptions.Queries
{
    public sealed class SubscriptionQueries
    {
        private readonly ISubscriptionRepository _repository;
        private readonly IReportingStore _reportingStore;
        private readonly IReportingQueries _reportingQueries;
        public SubscriptionQueries(
            ISubscriptionRepository repository,
            IReportingStore reportingStore,
            IReportingQueries reportingQueries)
        {
            _repository = repository;
            _reportingStore = reportingStore;
            _reportingQueries = reportingQueries;
        }
        public async Task<SubscriptionDetailsResponse> GetDetailsAsync(Guid id, CancellationToken ct = default)
        {
            var subscription = await _repository.GetByIdAsync(id, ct)
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
            var report = _reportingStore.GetOrAdd(customerId);

            var items = new List<SubscriptionListItem>();
            foreach(var subscriptionId in report.ActiveSubscriptionIds)
            {
                var subscription = await _repository.GetByIdAsync(subscriptionId, ct);
                if(subscription is null)
                {
                    continue;
                }
                items.Add(new SubscriptionListItem(
                    subscription.Id,
                    subscription.PlanCode,
                    subscription.Status.ToString()));
            }
            return items
                .OrderBy(x => x.PlanCode)
                .ToList();
        }

    }
}
