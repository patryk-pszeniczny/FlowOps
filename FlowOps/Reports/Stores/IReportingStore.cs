using FlowOps.Reports.Models;
namespace FlowOps.Reports.Stores
{
    public interface IReportingStore
    {
        CustomerReport UpsertOnActivation(Guid customerId);
        bool TryGet(Guid customerId, out CustomerReport? report);
        CustomerReport GetOrAdd(Guid customerId);
    }
}
