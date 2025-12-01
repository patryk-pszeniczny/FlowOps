using FlowOps.Contracts.Item;

namespace FlowOps.Services.Plans
{
    public interface IPlanCatalog
    {
        PlanInfoItem? TryGet(string planCode);

        IReadOnlyList<PlanInfoItem> GetAll();
    }
}
