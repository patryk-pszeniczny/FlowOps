using FlowOps.Application.UserData;
using FlowOps.Contracts.Request.UserState;
using FlowOps.Contracts.Response.UserState;
using Microsoft.AspNetCore.Mvc;

namespace FlowOps.Controllers
{
    [ApiController]
    [Route("api/user-state")]
    public class UserStateController : ControllerBase
    {
        private readonly UserStateService _service;
        private readonly ILogger<UserStateController> _logger;
        public UserStateController(
            UserStateService service,
            ILogger<UserStateController> logger)
        {
            _service = service;
            _logger = logger;
        }
        [HttpGet("{userId:guid}")]
        public async Task<ActionResult<UserStateResponse>> Get(Guid userId, CancellationToken ct)
        {
            var snapshot = await _service.GetAsync(userId, ct);
            return Ok(ToResponse(snapshot));
        }

        [HttpPost("{userId:guid}/preferences")]
        public async Task<IActionResult> SavePreference(Guid userId, [FromBody] SavePreferenceRequest request, CancellationToken ct)
        {
            await _service.SavePreferenceAsync(userId, request.Key, request.Value, DateTime.UtcNow, ct);
            _logger.LogInformation("Preference {Key} saved for {UserId}.", request.Key, userId);
            return NoContent();
        }

        [HttpPost("{userId:guid}/drafts")]
        public async Task<ActionResult<SubscriptionDraftItem>> SaveDraft(Guid userId, [FromBody] SaveDraftRequest request, CancellationToken ct)
        {
            var draftId = request.DraftId ?? Guid.NewGuid();
            await _service.SaveDraftAsync(draftId, userId, request.CustomerId, request.PlanCode, request.Payload, DateTime.UtcNow, ct);

            var snapshot = await _service.GetAsync(userId, ct);
            var draft = snapshot.Drafts.First(d => d.Id == draftId);

            return Ok(new SubscriptionDraftItem
            {
                DraftId = draft.Id,
                CustomerId = draft.CustomerId,
                PlanCode = draft.PlanCode,
                Payload = draft.Payload,
                LastUpdatedAt = draft.LastUpdatedAt
            });
        }

        [HttpPost("{userId:guid}/cached-lists")]
        public async Task<IActionResult> SaveCachedList(Guid userId, [FromBody] SaveCacheRequest request, CancellationToken ct)
        {
            await _service.SaveListCacheAsync(userId, request.CacheKey, request.Payload, DateTime.UtcNow, request.ExpiresAt, ct);
            return NoContent();
        }

        private static UserStateResponse ToResponse(UserStateSnapshot snapshot) => new()
        {
            UserId = snapshot.UserId,
            Preferences = snapshot.Preferences
                .Select(p => new UserPreferenceItem { Key = p.Key, Value = p.Value, UpdatedAt = p.UpdatedAt })
                .ToArray(),
            CachedLists = snapshot.CachedLists
                .Select(c => new CachedListItem { CacheKey = c.CacheKey, Payload = c.Payload, CachedAt = c.CachedAt, ExpiresAt = c.ExpiresAt })
                .ToArray(),
            Drafts = snapshot.Drafts
                .Select(d => new SubscriptionDraftItem
                {
                    DraftId = d.Id,
                    CustomerId = d.CustomerId,
                    PlanCode = d.PlanCode,
                    Payload = d.Payload,
                    LastUpdatedAt = d.LastUpdatedAt
                })
                .ToArray()
        };

    }
}
