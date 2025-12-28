using FlowOps.Contracts.Response;
using FlowOps.Events;
using FlowOps.Services.Replay;
using Microsoft.AspNetCore.Mvc;

namespace FlowOps.Controllers.Reporting
{

    [ApiController]
    [Route("api/analytics")]
    public class AnalyticsController : ControllerBase
    {
        private readonly EventRecorder _recorder;
        public AnalyticsController(
            EventRecorder recorder)
        {
            _recorder = recorder;
        }
        [HttpGet("activity")]
        public ActionResult<IEnumerable<object>> GetRecentActivity([FromQuery] int take = 50)
        {
            var events = _recorder
                .Snapshot()
                .OrderByDescending(ev => ev.OccurredOn)
                .ThenByDescending(ev => ev.Version)
                .Take(Math.Clamp(take, 1, 200))
                .Select(e => new
                {
                    type = e.GetType().Name,
                    e.Id,
                    e.OccurredOn,
                    e.Version
                });
            return Ok(events);

        }
        [HttpGet("billing")]
        public ActionResult<BillingSummaryResponse> GetBillingSummary()
        {
            var issued = _recorder.Snapshot().OfType<InvoiceIssuedEvent>().ToList();

            var paid = _recorder.Snapshot().OfType<InvoicePaidEvent>().ToList();

            var summary = new BillingSummaryResponse
            {
                TotalIssue = issued.Sum(i => i.Amount),
                TotalPaid = paid.Sum(p => p.Amount),
                OutstandingInvoices = issued.Count(i => paid.All(p => p.InvoiceId != i.InvoiceId)),
                CurrencyBreakdown = issued
                    .GroupBy(i => i.Currency)
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.Amount))
            };
            return Ok(summary);
        }
    }
}
