using FlowOps.Reports.Models;
using FlowOps.Reports.Stores;
using Microsoft.AspNetCore.Mvc;

namespace FlowOps.Controllers
{
    [ApiController]
    [Route("api/reports/customers")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportingStore _store;
        public ReportsController(IReportingStore store)
        {
            _store = store;
        }
        // path postman : GET http://localhost:32768/api/reports/customers/{customerId}
        [HttpGet("{customerId:guid}")]
        public ActionResult<CustomerReport> Get(Guid customerId){
            if (_store.TryGet(customerId, out var report) && report is not null)
            {
                return Ok(report);
            }
            return NotFound();
        }
        [HttpGet("customers/{custmerId:guid}/active-subscriptions")]
        public IActionResult GetActiveSubscriptionIds(Guid customerId)
        {
            var report = _store.GetOrAdd(customerId);
            var ids = report.ActiveSubscriptionIds.OrderBy(id => id).ToArray();
            return Ok(ids);
        }
        }
    }
}
