using Microsoft.AspNetCore.Mvc;
using FlowOps.Contracts.Request;
using FlowOps.Infrastructure.Idempotency;
using FlowOps.Application.Subscriptions.Commands;

namespace FlowOps.Controllers.Subscription
{
    [ApiController]
    [Route("api/[controller]")]
    [Consumes("application/json")]
    [Produces("application/json")]
    public class SubscriptionsController : ControllerBase
    {
        private readonly CreateSubscriptionCommandHandler _createHandler;
        private readonly CancelSubscriptionCommandHandler _cancelHandler;
        private readonly SuspendSubscriptionCommandHandler _suspendHandler;
        private readonly ResumeSubscriptionCommandHandler _resumeHandler;

        private readonly IIdempotencyStore _idempotency;
        private readonly ILogger<SubscriptionsController> _logger;
       
        public SubscriptionsController(
            CreateSubscriptionCommandHandler createHandler,
            CancelSubscriptionCommandHandler cancelHandler,
            SuspendSubscriptionCommandHandler suspendHandler,
            ResumeSubscriptionCommandHandler resumeHandler,
            IIdempotencyStore idempotency,
            ILogger<SubscriptionsController> logger)
        {
            _createHandler = createHandler;
            _cancelHandler = cancelHandler;
            _suspendHandler = suspendHandler;
            _resumeHandler = resumeHandler;
            _idempotency = idempotency;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateSubscriptionRequest request, CancellationToken ct)
        {
            var idempotencyKey = Request.Headers.TryGetValue("Idempotency-Key", out var vals) ? vals.ToString() : null;


            _logger.LogInformation(
                "CreateSubscription: customerId={CustomerId}, planCode={PlanCode}, idem={Idem}",
                request.CustomerId,
                request.PlanCode,
                idempotencyKey);

            if (!string.IsNullOrWhiteSpace(idempotencyKey) && _idempotency.TryGet(idempotencyKey, out var existringId))
            {
                return Ok(new
                {
                    message = "Subscription already created (idempotent).",
                    subscriptionId = existringId
                });
            }
            
            var id = await _createHandler.HandleAsync(new CreateSubscriptionCommand(request.CustomerId, request.PlanCode), ct);
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
            await _cancelHandler.HandleAsync(new CancelSubscriptionCommand(id), ct);
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
            await _suspendHandler.HandleAsync(new SuspendSubscriptionCommand(id), ct);
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
            await _resumeHandler.HandleAsync(new ResumeSubscriptionCommand(id), ct);
            return Ok(
                new
                {
                    message = "Subscription resumed",
                    subscriptionId = id
                });
        }
        [HttpGet("idempotency/{key}")]
        public ActionResult<object> InspectIdempotencyKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return BadRequest(new { message = "Idempotency key is required." });
            }

            if (_idempotency.TryGet(key, out var subscriptionId))
            {
                return Ok(new
                {
                    key,
                    subscriptionId,
                    status = "exists"
                });
            }

            return NotFound(new
            {
                key,
                status = "missing"
            });
        }

    }
}
