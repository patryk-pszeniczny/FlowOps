using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Contracts.Request;
using FlowOps.Events;
using FlowOps.Services.Replay;
using Microsoft.AspNetCore.Mvc;

namespace FlowOps.Controllers.Billing
{
    [ApiController]
    [Route("api/payments")]
    public class PaymentsController : ControllerBase
    {
        private readonly IEventBus _eventBus;
        private readonly EventRecorder _recoder;

        public PaymentsController(
            IEventBus eventBus,
            EventRecorder recoder)
        {
            _eventBus = eventBus;
            _recoder = recoder;
        }
        [HttpPost]
        public async Task<IActionResult> Pay([FromBody] PayInvoiceRequest request)
        {
            var invoiceId = request.InvoiceId == Guid.Empty ? Guid.NewGuid() : request.InvoiceId;

            var ev = new InvoicePaidEvent
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
            await _eventBus.PublishAsync(ev);

            return Ok(new
            { 
                message = "Payment received and event published.",
                invoiceId
            });
        }
        [HttpGet("history")]
        public ActionResult<IEnumerable<object>> GetHistory(
            [FromQuery] Guid? customerId,
            [FromQuery] Guid? subscriptionId)
        {
            var items = GetPaidEvents()
                .Where(ev => !customerId.HasValue || ev.CustomerId == customerId.Value)
                .Where(ev => !subscriptionId.HasValue || ev.SubscriptionId == subscriptionId.Value)
                .OrderByDescending(ev => ev.PaidAt)
                .Select(ev => new
                {
                    ev.InvoiceId,
                    ev.CustomerId,
                    ev.SubscriptionId,
                    ev.Amount,
                    ev.Currency,
                    ev.PaidAt,
                    ev.PaymentMethod,
                    ev.TransactionId
                })
                .ToList();
            return Ok(items);
        }
        [HttpGet("stats")]
        public ActionResult<object> GetStats()
        {
            var paid = GetPaidEvents();
            var byCurrency = paid
                .GroupBy(ev => ev.Currency)
                .Select(g => new
                {
                    currency = g.Key,
                    totalPaid = g.Sum(ev => ev.Amount),
                    count = g.Count()
                })
                .OrderByDescending(x => x.totalPaid)
                .ToList();
            return Ok(new
            {
                totalPayments = paid.Count,
                totalAmount = paid.Sum(ev => ev.Amount),
                byCurrency
            });
        }
        private IReadOnlyList<InvoicePaidEvent> GetPaidEvents()
        {
            return _recoder
                .Snapshot()
                .OfType<InvoicePaidEvent>()
                .ToList();
        }
    }
}
