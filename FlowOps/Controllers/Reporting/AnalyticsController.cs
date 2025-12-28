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

        [HttpGet("subscriptions/status")]
        public ActionResult<object> GetSubscriptionStatusSummary()
        {
            var statusBySubscription = new Dictionary<Guid, (string Status, string Plan)>();

            foreach (var ev in _recorder.Snapshot().OrderBy(e => e.OccurredOn).ThenBy(e => e.Version))
            {
                switch (ev)
                {
                    case SubscriptionActivatedEvent activated:
                        statusBySubscription[activated.SubscriptionId] = ("Active", activated.PlanCode);
                        break;
                    case SubscriptionSuspendedEvent suspended:
                        statusBySubscription[suspended.SubscriptionId] = ("Suspended", suspended.PlanCode);
                        break;
                    case SubscriptionResumedEvent resumed:
                        statusBySubscription[resumed.SubscriptionId] = ("Active", resumed.PlanCode);
                        break;
                    case SubscriptionCancelledEvent cancelled:
                        statusBySubscription[cancelled.SubscriptionId] = ("Cancelled", cancelled.PlanCode);
                        break;
                }
            }

            var byStatus = statusBySubscription
                .GroupBy(kv => kv.Value.Status)
                .Select(g => new
                {
                    status = g.Key,
                    count = g.Count()
                })
                .OrderByDescending(x => x.count)
                .ToArray();

            var byPlan = statusBySubscription
                .GroupBy(kv => kv.Value.Plan)
                .Select(g => new
                {
                    planCode = g.Key,
                    active = g.Count(x => x.Value.Status == "Active"),
                    suspended = g.Count(x => x.Value.Status == "Suspended"),
                    cancelled = g.Count(x => x.Value.Status == "Cancelled"),
                    total = g.Count(),
                })
                .OrderByDescending(x => x.total)
                .ToArray();

            return Ok(new
            {
                total = statusBySubscription.Count,
                byStatus,
                byPlan
            });
        }

        [HttpGet("subscriptions/velocity")]
        public ActionResult<IEnumerable<object>> GetSubscriptionVelocity([FromQuery] int days = 30)
        {
            var window = Math.Clamp(days, 1, 365);
            var since = DateTime.UtcNow.Date.AddDays(-window);

            var events = _recorder
                .Snapshot()
                .Where(ev => ev.OccurredOn.Date >= since)
                .GroupBy(ev => ev.OccurredOn.Date)
                .Select(g => new
                {
                    date = g.Key,
                    activated = g.Count(e => e is SubscriptionActivatedEvent),
                    suspended = g.Count(e => e is SubscriptionSuspendedEvent),
                    resumed = g.Count(e => e is SubscriptionResumedEvent),
                    cancelled = g.Count(e => e is SubscriptionCancelledEvent)
                })
                .OrderBy(g => g.date)
                .ToArray();

            return Ok(events);
        }

        [HttpGet("billing/trends")]
        public ActionResult<IEnumerable<object>> GetBillingTrends([FromQuery] int months = 6)
        {
            var window = Math.Clamp(months, 1, 24);
            var since = DateTime.UtcNow.AddMonths(-window);

            var issued = _recorder.Snapshot().OfType<InvoiceIssuedEvent>().Where(i => i.OccurredOn >= since).ToList();
            var paid = _recorder.Snapshot().OfType<InvoicePaidEvent>().Where(i => i.OccurredOn >= since).ToList();

            var issuedByMonth = issued
                .GroupBy(i => new { i.OccurredOn.Year, i.OccurredOn.Month })
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Amount));

            var paidByMonth = paid
                .GroupBy(i => new { i.OccurredOn.Year, i.OccurredOn.Month })
                .ToDictionary(g => g.Key, g => g.Sum(i => i.Amount));

            var monthsRange = Enumerable
                .Range(0, window)
                .Select(offset => DateTime.UtcNow.AddMonths(-offset))
                .Select(d => new { d.Year, d.Month })
                .Distinct()
                .OrderBy(x => x.Year)
                .ThenBy(x => x.Month)
                .ToArray();

            var trends = monthsRange
                .Select(m => new
                {
                    year = m.Year,
                    month = m.Month,
                    issuedAmount = issuedByMonth.TryGetValue(m, out var iss) ? iss : 0,
                    paidAmount = paidByMonth.TryGetValue(m, out var p) ? p : 0,
                    collectionRate = issuedByMonth.TryGetValue(m, out var issuedAmount) && issuedAmount > 0
                        ? Math.Round((paidByMonth.TryGetValue(m, out var paidAmount) ? paidAmount : 0) / issuedAmount * 100, 2)
                        : 0
                })
                .ToArray();

            return Ok(trends);
        }
    }
}
