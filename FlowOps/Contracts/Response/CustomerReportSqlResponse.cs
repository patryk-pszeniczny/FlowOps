namespace FlowOps.Contracts.Response
{
    public sealed record CustomerReportSqlResponse
    (
        Guid CustomerId,
        int ActiveSubscriptions,
        decimal TotalInvoiced,
        decimal TotalPaid
    );
    
}
