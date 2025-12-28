using System.Linq;
using FlowOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowOps.Controllers.Diagnostics
{
    [ApiController]
    [Route("api/diagnostics/integration-events")]
    public class IntegrationEventsController : ControllerBase
    {
        private readonly FlowOpsDbContext _dbContext;
        private readonly ILogger<IntegrationEventsController> _logger;

        public IntegrationEventsController(
            FlowOpsDbContext dbContext,
            ILogger<IntegrationEventsController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetRecent(
            [FromQuery] int take = 50,
            CancellationToken ct = default)
        {
            var sanitized = Math.Clamp(take, 1, 500);
            var events = await _dbContext.IntegrationEvents
                .OrderByDescending(e => e.OccurredAt)
                .Take(sanitized)
                .Select(e => new
                {
                    e.Id,
                    e.TypeName,
                    e.OccurredAt,
                    e.Version
                })
                .ToListAsync(ct);

            return Ok(events);
        }

        [HttpGet("summary")]
        public async Task<ActionResult<object>> GetSummary(CancellationToken ct)
        {
            var grouped = await _dbContext.IntegrationEvents
                .GroupBy(e => e.TypeName)
                .Select(g => new
                {
                    type = g.Key,
                    count = g.Count(),
                    firstSeen = g.Min(e => e.OccurredAt),
                    lastSeen = g.Max(e => e.OccurredAt)
                })
                .OrderByDescending(g => g.count)
                .ToListAsync(ct);

            var total = await _dbContext.IntegrationEvents.CountAsync(ct);

            return Ok(new
            {
                total,
                grouped
            });
        }

        [HttpDelete]
        public async Task<IActionResult> Purge(CancellationToken ct)
        {
            var events = await _dbContext.IntegrationEvents.ToListAsync(ct);
            if (events.Count == 0)
            {
                return NoContent();
            }

            _dbContext.IntegrationEvents.RemoveRange(events);
            await _dbContext.SaveChangesAsync(ct);

            _logger.LogWarning("Purged {Count} integration events from persistent store.", events.Count);

            return Ok(new
            {
                message = "Integration events removed from store.",
                removed = events.Count
            });
        }
    }
}