using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Events;
using Microsoft.AspNetCore.Mvc;
using FlowOps.Domain.Subscriptions;
using FlowOps.Application.Subscriptions;
using FlowOps.Contracts.Request;
using FlowOps.Infrastructure.Idempotency;

namespace FlowOps.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public class SubscriptionsController : ControllerBase
    {
        private readonly SubscriptionCommandService _service;
        private readonly IIdempotencyStore _idempotency;
        public SubscriptionsController(SubscriptionCommandService service, IIdempotencyStore idempotency)
        {
            _service = service;
            _idempotency = idempotency;
        }
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSubscriptionRequest request)
        {
            var idempotencyKey = Request.Headers.TryGetValue("Idempotency-Key", out var vals) ? vals.ToString() : null;

            if (!string.IsNullOrWhiteSpace(idempotencyKey) && _idempotency.TryGet(idempotencyKey, out var existringId))
            {
                return Ok(new
                {
                    message = "Subscription already created (idempotent).",
                    subscriptionId = existringId
                });
            }
            
            var id = await _service.CreateAsync(request.CustomerId, request.PlanCode, DateTime.UtcNow);
            if(!string.IsNullOrWhiteSpace(idempotencyKey))
            {
                _idempotency.Set(idempotencyKey, id);
            }
            return Ok(
                new
                {
                    message = "Subscription created and event published.",
                    subscriptionId = id
                }
            );
        }
        [HttpPost("{id:guid}/cancel")]
        public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
        {
            await _service.CancelAsync(id, DateTime.UtcNow, ct);
            return Ok(
                new
                {
                    message = "Subscription cancelled",
                    subscriptionId = id
                }
            );
        }
        [HttpPost("{id:guid}/suspend")]
        public async Task<IActionResult> Suspend(Guid id, CancellationToken ct)
        {
            await _service.SuspendAsync(id, DateTime.UtcNow, ct);
            return Ok(
                new
                {
                    message = "Subscription suspended",
                    subscriptionId = id
                });
        }
        [HttpPost("{id:guid}/resume")]
        public async Task<IActionResult> Resume(Guid id, CancellationToken ct)
        {
            await _service.ResumeAsync(id, DateTime.UtcNow, ct);
            return Ok(
                new
                {
                    message = "Subscription resumed",
                    subscriptionId = id
                });
        }

    }
}
