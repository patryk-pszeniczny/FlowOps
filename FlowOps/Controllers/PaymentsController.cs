using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Contracts.Request;
using FlowOps.Events;
using Microsoft.AspNetCore.Mvc;

namespace FlowOps.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IEventBus _eventBus;
        public PaymentsController(IEventBus eventBus)
        {
            _eventBus = eventBus;
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
    }
}
