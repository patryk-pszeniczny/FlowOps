namespace FlowOps.Contracts.Response
{
    public class BillingSummaryResponse
    {
        public decimal TotalIssue { get; set; }
        public decimal TotalPaid { get; set; }
        public int OutstandingInvoices { get; set; }
        public Dictionary<string, decimal> CurrencyBreakdown { get; set; } = new();
    }
}
