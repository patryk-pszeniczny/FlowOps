namespace FlowOps.Contracts.Request.Customers
{
    public sealed class CreateCustomerRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? TaxId { get; set; }
        public string? Email { get; set; }
    }
}
