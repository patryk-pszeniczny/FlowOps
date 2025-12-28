using FlowOps.Contracts.Request.Idempotency;
using FlowOps.Infrastructure.Persistence;
using FlowOps.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowOps.Controllers.Diagnostics
{
    [ApiController]
    [Route("api/diagnostics/idempotency")]
    public class IdempotencyController : ControllerBase
    {
        private readonly FlowOpsDbContext _dbContext;
        private readonly ILogger<IdempotencyController> _logger;

        public IdempotencyController(FlowOpsDbContext dbContext, ILogger<IdempotencyController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> List(CancellationToken ct)
        {
            var keys = await _dbContext.IdempotencyKeys
                .OrderBy(k => k.Key)
                .Select(k => new
                {
                    k.Key,
                    k.SubscriptionId
                })
                .ToListAsync(ct);

            return Ok(keys);
        }

        [HttpGet("{key}")]
        public async Task<ActionResult<object>> Get(string key, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return BadRequest(new { message = "Key is required." });
            }

            var entry = await _dbContext.IdempotencyKeys.FindAsync(new object[] { key }, ct);
            if (entry is null)
            {
                return NotFound(new { message = "Key not found.", key });
            }

            return Ok(new
            {
                entry.Key,
                entry.SubscriptionId
            });
        }

        [HttpPost]
        public async Task<IActionResult> Upsert(UpsertIdempotencyKeyRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Key))
            {
                return BadRequest(new { message = "Key cannot be empty." });
            }

            var existing = await _dbContext.IdempotencyKeys.FindAsync(new object[] { request.Key }, ct);
            if (existing is null)
            {
                var entity = new IdempotencyKeyEntity
                {
                    Key = request.Key,
                    SubscriptionId = request.SubscriptionId
                };
                _dbContext.IdempotencyKeys.Add(entity);
                await _dbContext.SaveChangesAsync(ct);

                _logger.LogInformation("Created idempotency key {Key} for subscription {SubscriptionId}.", request.Key, request.SubscriptionId);

                return CreatedAtAction(nameof(Get), new { key = request.Key }, new { request.Key, request.SubscriptionId });
            }

            existing.SubscriptionId = request.SubscriptionId;
            await _dbContext.SaveChangesAsync(ct);
            _logger.LogInformation("Updated idempotency key {Key} for subscription {SubscriptionId}.", request.Key, request.SubscriptionId);

            return Ok(new { request.Key, request.SubscriptionId });
        }

        [HttpDelete("{key}")]
        public async Task<IActionResult> Delete(string key, CancellationToken ct)
        {
            var existing = await _dbContext.IdempotencyKeys.FindAsync(new object[] { key }, ct);
            if (existing is null)
            {
                return NotFound(new { message = "Key not found.", key });
            }

            _dbContext.IdempotencyKeys.Remove(existing);
            await _dbContext.SaveChangesAsync(ct);

            return Ok(new { message = "Idempotency key removed.", key });
        }

        [HttpDelete]
        public async Task<IActionResult> Clear(CancellationToken ct)
        {
            var keys = await _dbContext.IdempotencyKeys.ToListAsync(ct);
            if (keys.Count == 0)
            {
                return NoContent();
            }

            _dbContext.IdempotencyKeys.RemoveRange(keys);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogWarning("Cleared {Count} idempotency keys.", keys.Count);

            return Ok(new { message = "All idempotency keys cleared.", removed = keys.Count });
        }
    }
}