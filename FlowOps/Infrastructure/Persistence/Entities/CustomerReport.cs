namespace FlowOps.Infrastructure.Persistence.Entities
{
    public sealed class CustomerReport
    {
        public Guid CustomerId { get; set; }
        public int ActiveSubscriptions { get; set; }
        public decimal TotalInvoiced { get; set; }
        public decimal TotalPaid { get; set; }
    }
}
