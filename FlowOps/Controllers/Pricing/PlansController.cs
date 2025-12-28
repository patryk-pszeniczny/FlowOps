using FlowOps.Contracts.Response;
using FlowOps.Domain.Plans;
using Microsoft.AspNetCore.Mvc;

namespace FlowOps.Controllers.Pricing
{
    [ApiController]
    [Route("api/plans")]
    public class PlansController : ControllerBase
    {
        private readonly IPlanPricing _pricing;

        public PlansController(
            IPlanPricing pricing)
        {
            _pricing = pricing;
        }
        [HttpGet]
        public ActionResult<IEnumerable<PlanResponse>> Get()
        {
            var items = _pricing
                .GetAll()
                .Select(kv => new PlanResponse(kv.Key, kv.Value))
                .OrderBy(p => p.Code)
                .ToList();
            return Ok(items);
        }
        [HttpGet("{planCode}")]
        public ActionResult<PlanResponse> GetByCode(string planCode)
        {
            var all = _pricing.GetAll();
            if (!all.TryGetValue(planCode, out var price))
            {
                return NotFound();
            }
            return Ok(new PlanResponse(planCode, price));
        }
        [HttpGet("recommendation")]
        public ActionResult<PlanResponse> Recommend([FromQuery] decimal budget)
        {
            var plan = _pricing
                .GetAll()
                .Select(kv => new PlanResponse(kv.Key, kv.Value))
                .Where(p => p.Price <= budget)
                .OrderByDescending(p => p.Price)
                .FirstOrDefault();

            if (plan is null)
            {
                return NotFound(new
                {
                    message = "No plans available within the specified budget."
                });
            }
            return Ok(plan);
        }
    }
}
