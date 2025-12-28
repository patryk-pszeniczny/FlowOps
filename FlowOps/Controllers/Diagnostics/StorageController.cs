using FlowOps.Services.Replay;
using FlowOps.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FlowOps.Controllers.Diagnostics
{
    [ApiController]
    [Route("api/diagnostics/storage")]
    public class StorageController : ControllerBase
    {
        private readonly FlowOpsDbContext _dbContext;
        private readonly EventRecorder _recorder;

        public StorageController(FlowOpsDbContext dbContext, EventRecorder recorder)
        {
            _dbContext = dbContext;
            _recorder = recorder;
        }

        [HttpGet("overview")]
        public async Task<ActionResult<object>> GetOverview(CancellationToken ct)
        {
            var customers = await _dbContext.Customers.CountAsync(ct);
            var subscriptions = await _dbContext.Subscriptions.CountAsync(ct);
            var idempotencyKeys = await _dbContext.IdempotencyKeys.CountAsync(ct);
            var integrationEvents = await _dbContext.IntegrationEvents.CountAsync(ct);
            var inbox = await _dbContext.InboxMessages.CountAsync(ct);
            var outbox = await _dbContext.OutboxMessages.CountAsync(ct);
            var recordedEvents = _recorder.Snapshot().Count;

            return Ok(new
            {
                customers,
                subscriptions,
                idempotencyKeys,
                integrationEvents,
                inbox,
                outbox,
                recordedEvents
            });
        }

        [HttpGet("user-state")]
        public async Task<ActionResult<object>> GetUserStateStats(CancellationToken ct)
        {
            var preferences = await _dbContext.UserPreferences.CountAsync(ct);
            var cachedLists = await _dbContext.UserListCaches.CountAsync(ct);
            var drafts = await _dbContext.SubscriptionDrafts.CountAsync(ct);

            return Ok(new
            {
                preferences,
                cachedLists,
                drafts
            });
        }
    }
}