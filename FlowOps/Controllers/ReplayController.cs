using FlowOps.Events;
using FlowOps.Reports.Stores;
using FlowOps.Services.Replay;
using FlowOps.Services.Reporting;
using Microsoft.AspNetCore.Mvc;

namespace FlowOps.Controllers
{
    [ApiController]
    [Route("api/replay")]
    public class ReplayController : ControllerBase
    {
        private readonly EventRecoder _recoder;
        private readonly IReportingHandler _reporting;
        private readonly IReportingStore _store;

        public ReplayController(
            EventRecoder recoder, 
            IReportingHandler reporting, 
            IReportingStore store)
        {
            _recoder = recoder;
            _reporting = reporting;
            _store = store;
        }
        [HttpGet("events")]
        public ActionResult<IEnumerable<object>> GetEvents()
        {
            var snapshot = _recoder.Snapshot();
            var snaped = snapshot.Select(e => new
            {
                type = e.GetType().Name,
                e.Id,
                e.OccuredOn,
                e.Version
            });
            return Ok(snaped);
        }
        [HttpPost("reports/rebuild")]
        public async Task<IActionResult> RebuildReports(CancellationToken ct)
        {
            if(_store is InMemoryReportingStore mem)
            {
                var field = typeof(InMemoryReportingStore).GetField("_data", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var dict = field?.GetValue(mem) as System.Collections.IDictionary;
                dict?.Clear();
            }
            foreach (var ev in _recoder.Snapshot())
            {
                switch (ev)
                {
                    case SubscriptionActivatedEvent a:
                        await _reporting.On(a, ct);
                        break;
                    case SubscriptionCancelledEvent c:
                        await _reporting.On(c, ct);
                        break;
                    case InvoiceIssuedEvent i:
                        await _reporting.On(i, ct);
                        break;
                    case InvoicePaidEvent p:
                        await _reporting.On(p, ct);
                        break;
                }
            }
            return Ok(new
            {
                message = "Reports rebuild from recorded events."
            });
        }
    }
}
