namespace FlowOps.Domain.Plans
{
    public interface IPlanPricing
    {
        decimal GetPrice(string planCode);
        IReadOnlyDictionary<string, decimal> GetAll();
    }
}
