namespace FlowOps.Infrastructure.Customer
{
    public sealed class CustomerEntity
    {
        public Guid CustomerId { get; set; }
        public string Name { get; set; } = string.Empty;

        public string? TaxId { get; set; }

        public string? Email { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
