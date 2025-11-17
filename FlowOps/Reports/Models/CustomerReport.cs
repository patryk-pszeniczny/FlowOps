namespace FlowOps.Reports.Models
{
    public class CustomerReport
    {
        public Guid CustomerId { get; set; }
        public int ActiveSubscriptions { get; set; }
        public decimal TotalInvoiced { get; set; }
        public decimal TotalPaid { get; set; }
    }
}
