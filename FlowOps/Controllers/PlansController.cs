using FlowOps.Contracts.Response;
using FlowOps.Pricing;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FlowOps.Controllers
{
    [Route("api/plans")]
    [ApiController]
    public class PlansController : ControllerBase
    {
        private readonly IPlanPricing _pricing;
        public PlansController(IPlanPricing pricing)
        {
            _pricing = pricing;
        }
        [HttpGet]
        public ActionResult<IEnumerable<PlanResponse>> Get()
        {
            var items = _pricing
                .GetAll()
                .Select(kv=> new PlanResponse(kv.Key, kv.Value))
                .OrderBy(p => p.Price)
                .ToList();
            return Ok(items);
        }
    }
}
