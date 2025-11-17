using FlowOps.Reports.Models;
using System.Collections.Concurrent;

namespace FlowOps.Reports.Stores
{
    public class InMemoryReportingStore : IReportingStore
    {
        private readonly ConcurrentDictionary<Guid, CustomerReport> _data = new();
        public CustomerReport UpsertOnActivation(Guid customerId)
        {
           return _data.AddOrUpdate(
                customerId,
                id => new CustomerReport
                {
                    CustomerId = id,
                    ActiveSubscriptions = 1,
                    TotalInvoiced = 0,
                    TotalPaid = 0
                },
                (id, existingReport) =>
                {
                    existingReport.ActiveSubscriptions += 1;
                    return existingReport;
                });
        }

        public bool TryGet(Guid customerId, out CustomerReport? report) 
            => _data.TryGetValue(customerId, out report);

        public CustomerReport GetOrAdd(Guid customerId)
        {
            return _data.GetOrAdd(customerId, id => new CustomerReport
            {
                CustomerId = id,
                ActiveSubscriptions = 0,
                TotalInvoiced = 0,
                TotalPaid = 0
            });
        }
    }
}
