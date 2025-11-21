using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FlowOps.Pricing
{
    public interface IPlanPricing
    {
        decimal GetPrice(string planCode);
        IReadOnlyDictionary<string, decimal> GetAll();
    }
}
