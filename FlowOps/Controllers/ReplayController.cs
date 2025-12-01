using FlowOps.BuildingBlocks.Messaging;
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
        private readonly IIntegrationEventStore _eventStore;
        private readonly IReportingStore _reportingStore;
        private readonly IReportingHandler _reportingHandler;
        private readonly ILogger<ReplayController> _logger;
        public ReplayController(
            IIntegrationEventStore eventStore,
            IReportingStore reportingStore,
            IReportingHandler reportingHandler,
            ILogger<ReplayController> logger)
        {
            _eventStore = eventStore;
            _reportingStore = reportingStore;
            _reportingHandler = reportingHandler;
            _logger = logger;
        }
        [HttpPost("reports/rebuild")]
        public async Task<IActionResult> RebuildReports(CancellationToken ct)
        {
            _logger.LogInformation("Starting SQL replay of reports from IntegrationEvents...");

            var events = await _eventStore.GetAllAsync(ct);
            _logger.LogInformation("Replaying {Count} events from SQL event store.", events.Count);

            var handled = 0;

            foreach (var ev in events)
            {
                switch (ev)
                {
                    case SubscriptionActivatedEvent a:
                        await _reportingHandler.On(a, ct);
                        handled++;
                        break;
                    case SubscriptionCancelledEvent c:
                        await _reportingHandler.On(c, ct);
                        handled++;
                        break;
                    case SubscriptionSuspendedEvent s:
                        await _reportingHandler.On(s, ct);
                        handled++;
                        break;
                    case SubscriptionResumedEvent r:
                        await _reportingHandler.On(r, ct);
                        handled++;
                        break;
                    case InvoiceIssuedEvent i:
                        await _reportingHandler.On(i, ct);
                        handled++;
                        break;
                    case InvoicePaidEvent p:
                        await _reportingHandler.On(p, ct);
                        handled++;
                        break;
                    default:
                        _logger.LogDebug("Ignoring event of type {EventType} in replay.", ev.GetType().FullName ?? ev.GetType().Name);
                        break;
                }
            }
            _logger.LogInformation(
                "SQL replay finished. Total events: {Total}, handled by reporting: {Handled}.",
                events.Count,
                handled);
            return Ok(new
            {
                message = "Reports rebuild from SQL event store.",
                totalEvents = events.Count,
                handledEvents = handled
            });
        }
        //private readonly EventRecorder _recorder;
        //private readonly IReportingHandler _reporting;
        //private readonly IReportingStore _store;

        //public ReplayController(
        //    EventRecorder recorder, 
        //    IReportingHandler reporting, 
        //    IReportingStore store)
        //{
        //    _recorder = recorder;
        //    _reporting = reporting;
        //    _store = store;
        //}
        //[HttpGet("events")]
        //public ActionResult<IEnumerable<object>> GetEvents()
        //{
        //    var snapshot = _recorder.Snapshot()
        //        .OrderBy(e => e.OccurredOn)
        //        .ThenBy(e => e.Version)
        //        .Select(e => new
        //        {
        //            type = e.GetType().Name,
        //            e.Id,
        //            e.OccurredOn,
        //            e.Version
        //        });
        //    return Ok(snapshot);
        //}
        //[HttpPost("reports/rebuild")]
        //public async Task<IActionResult> RebuildReports(CancellationToken ct)
        //{
        //    if(_store is InMemoryReportingStore mem)
        //    {
        //        mem.Clear();
        //    }
        //    var ordered = _recorder.Snapshot()
        //        .OrderBy(e => e.OccurredOn)
        //        .ThenBy(e => e.Version);
        //    foreach (var ev in ordered)
        //    {
        //        switch (ev)
        //        {
        //            case SubscriptionActivatedEvent a:
        //                await _reporting.On(a, ct);
        //                break;
        //            case SubscriptionCancelledEvent c:
        //                await _reporting.On(c, ct);
        //                break;
        //            case InvoiceIssuedEvent i:
        //                await _reporting.On(i, ct);
        //                break;
        //            case InvoicePaidEvent p:
        //                await _reporting.On(p, ct);
        //                break;
        //            case SubscriptionSuspendedEvent s:
        //                await _reporting.On(s, ct);
        //                break;
        //            case SubscriptionResumedEvent r:
        //                await _reporting.On(r, ct);
        //                break;
        //        }
        //    }
        //    return Ok(new
        //    {
        //        message = "Reports rebuild from recorded events."
        //    });
        //}
    }
}
