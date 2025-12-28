using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Contracts.Request;
using FlowOps.Contracts.Response;
using FlowOps.Domain.Plans;
using FlowOps.Events;
using FlowOps.Services.Replay;
using Microsoft.AspNetCore.Mvc;

namespace FlowOps.Controllers.Billing
{
    [ApiController]
    [Route("api/invoices")]
    public class InvoicesController : ControllerBase
    {
        private readonly IEventBus _eventBus;
        private readonly EventRecorder _recoder;
        private readonly IPlanPricing _pricing;
    
        public InvoicesController(
            IEventBus eventBus,
            EventRecorder recoder,
            IPlanPricing pricing)
        {
            _eventBus = eventBus;
            _recoder = recoder;
            _pricing = pricing;
        }
        [HttpPost("issue")]
        public async Task<IActionResult> IssueAsync([FromBody] IssueInvoiceRequest request)
        {
            var price = request.Amount ?? _pricing.GetPrice(request.PlanCode);
            var invoiceId = request.InvoiceId == Guid.Empty ? Guid.NewGuid() : request.InvoiceId;

            var issued = new InvoiceIssuedEvent
            {
                InvoiceId = invoiceId,
                CustomerId = request.CustomerId,
                SubscriptionId = request.SubscriptionId,
                PlanCode = request.PlanCode,
                Amount = price,
                Currency = string.IsNullOrWhiteSpace(request.Currency) ? "PLN" : request.Currency!,
                IssuedAt = DateTime.UtcNow
            };

            await _eventBus.PublishAsync(issued);

            return Accepted(new
            {
                message = "Invoice issued and event published.",
                invoiceId
            });
        }

        [HttpPost("{invoiceId:guid}/pay")]
        public async Task<IActionResult> MarkPaid(Guid invoiceId, [FromBody] PayInvoiceRequest request)
        {
            var mergedRequest = new InvoicePaidEvent
            {
                InvoiceId = invoiceId,
                CustomerId = request.CustomerId,
                SubscriptionId = request.SubscriptionId,

                Amount = request.Amount,
                Currency = string.IsNullOrWhiteSpace(request.Currency) ? "PLN" : request.Currency!,
                PaidAt = DateTime.UtcNow,

                PaymentMethod = request.PaymentMethod,
                TransactionId = request.TransactionId
            };

            await _eventBus.PublishAsync(mergedRequest);

            return Ok(new
            {
                message = "Invoice marked as paid and event published.",
                invoiceId
            });
        }
        [HttpGet]
        public ActionResult<IEnumerable<InvoiceDetailsResponse>> GetAll(
            [FromQuery] Guid? customerId,
            [FromQuery] Guid? subscriptionId,
            [FromQuery] string? status)
        {
            var invoices = BuildInvoices();

            if(customerId.HasValue)
            {
                invoices = invoices.Where(i => i.CustomerId == customerId.Value).ToList();
            }
            if(subscriptionId.HasValue)
            {
                invoices = invoices.Where(i => i.SubscriptionId == subscriptionId.Value).ToList();
            }
            if(!string.IsNullOrWhiteSpace(status))
            {
                var allowed = status.Trim().ToLowerInvariant();
                if(allowed is "issued" or "paid")
                {
                    invoices = invoices.Where(i => string.Equals(i.Status, allowed, StringComparison.OrdinalIgnoreCase)).ToList();
                }
                else
                {
                    return BadRequest("Unknown status filter. Allowed values: issued, paid.");
                }
            }
            return Ok(invoices.OrderByDescending(invoices => invoices.IssuedAt));
        }
        [HttpGet("{invoiceId:guid}")]
        public ActionResult<InvoiceDetailsResponse> GetById(Guid invoiceId)
        {
            var invoices = BuildInvoices().FirstOrDefault(i => i.InvoiceId == invoiceId);
            if (invoices is null)
            {
                return NotFound();
            }
            return Ok(invoices);
        }
        [HttpGet("summary")]
        public ActionResult<BillingSummaryResponse> GetSummary()
        {
            var invoices = BuildInvoices();
            var summary = new BillingSummaryResponse
            {
                TotalIssue = invoices.Sum(i => i.Amount),
                TotalPaid = invoices.Where(i => i.Status == "Paid").Sum(i => i.Amount),
                CurrencyBreakdown = invoices
                    .GroupBy(i => i.Currency)
                    .ToDictionary(g => g.Key, g => g.Sum(i => i.Amount)),
                OutstandingInvoices = invoices.Count(i => i.Status == "Issued")
            };
            return Ok(summary);
        }
        private List<InvoiceDetailsResponse> BuildInvoices()
        {
            var issuedEvents = _recoder.Snapshot().OfType<InvoiceIssuedEvent>().ToList();
            var paidEvents = _recoder.Snapshot().OfType<InvoicePaidEvent>().ToList();

            var invoices = issuedEvents.Select(issued => new InvoiceDetailsResponse
            {
                InvoiceId = issued.InvoiceId,
                CustomerId = issued.CustomerId,
                SubscriptionId = issued.SubscriptionId,

                PlanCode = issued.PlanCode,
                Amount = issued.Amount,
                Currency = issued.Currency,

                IssuedAt = issued.IssuedAt,
                Status = paidEvents.Any(paid => paid.InvoiceId == issued.InvoiceId) ? "Paid" : "Issued",
                PaidAt = paidEvents.FirstOrDefault(paid => paid.InvoiceId == issued.InvoiceId)?.PaidAt
            }).ToList();

            foreach(var paidOnly in paidEvents.Where(p => invoices.All(i => i.InvoiceId != p.InvoiceId)))
            {
                invoices.Add(new InvoiceDetailsResponse
                {
                    InvoiceId = paidOnly.InvoiceId,
                    CustomerId = paidOnly.CustomerId,
                    SubscriptionId = paidOnly.SubscriptionId,
                    Amount = paidOnly.Amount,
                    Currency = paidOnly.Currency,
                    IssuedAt = DateTime.MinValue,
                    Status = "Paid",
                    PaidAt = paidOnly.PaidAt
                });
            }
            return invoices;
        }
    }
}
