using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Events;
using FlowOps.Reports.Stores;
using FlowOps.Services.Replay;
using FlowOps.Services.Reporting;
using Microsoft.AspNetCore.Mvc;

namespace FlowOps.Controllers.Reporting
{
    [ApiController]
    [Route("api/replay")]
    public class ReplayController : ControllerBase
    {
        private readonly EventRecorder _recorder;
        private readonly IReportingHandler _reportingHandler;
        private readonly IReportingStore _store;
        public ReplayController(
            EventRecorder recorder,
            IReportingHandler reportingHandler,
            IReportingStore store)
        {
            _recorder = recorder;
            _reportingHandler = reportingHandler;
            _store = store;
        }

        [HttpGet("events")]
        public ActionResult<IEnumerable<object>> GetEvents()
        {
            var snapshot = _recorder.Snapshot()
                .OrderBy(e => e.OccurredOn)
                .ThenBy(e => e.Version)
                .Select(e => new
                {
                    type = e.GetType().Name,
                    e.Id,
                    e.OccurredOn,
                    e.Version
                });
            return Ok(snapshot);
        }

        [HttpPost("reports/rebuild")]
        public async Task<IActionResult> RebuildReports(CancellationToken ct)
        {
            if (_store is InMemoryReportingStore mem)
            {
                mem.Clear();
            }
            var ordered = _recorder.Snapshot()
                .OrderBy(e => e.OccurredOn)
                .ThenBy(e => e.Version);

            foreach (var ev in ordered)
            {
                switch (ev)
                {
                    case SubscriptionActivatedEvent a:
                        await _reportingHandler.On(a, ct);
                        break;
                    case SubscriptionCancelledEvent c:
                        await _reportingHandler.On(c, ct);
                        break;
                    case SubscriptionSuspendedEvent s:
                        await _reportingHandler.On(s, ct);
                        break;
                    case SubscriptionResumedEvent r:
                        await _reportingHandler.On(r, ct);
                        break;
                    case InvoiceIssuedEvent i:
                        await _reportingHandler.On(i, ct);
                        break;
                    case InvoicePaidEvent p:
                        await _reportingHandler.On(p, ct);
                        break;
                }
            }
            return Ok(new
            {
                message = "Reports rebuild from recorded events."
            });
        }
        [HttpDelete("events")]
        public IActionResult ClearEvents()
        {
            _recorder.Clear();
            return Ok(new
            {
                message = "Recorded events cleared."
            });
        }
    }
}
