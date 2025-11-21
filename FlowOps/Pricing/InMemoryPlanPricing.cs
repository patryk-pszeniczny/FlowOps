namespace FlowOps.Pricing
{
    public sealed class InMemoryPlanPricing : IPlanPricing
    {
        private static readonly IReadOnlyDictionary<string, decimal> _prices =
            new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            { "BASIC", 9.99m },
            { "STANDARD", 19.99m },
            { "PREMIUM", 29.99m }
        };

        public IReadOnlyDictionary<string, decimal> GetAll() => _prices;

        public decimal GetPrice(string planCode)
        {
            if (string.IsNullOrWhiteSpace(planCode))
            {
                throw new InvalidOperationException("Plan code is required.");
            }
            if (_prices.TryGetValue(planCode, out var price))
            {
                return price;
            }
            throw new InvalidOperationException($"Unknown plan code: '{planCode}'.");
        }
    }
}
