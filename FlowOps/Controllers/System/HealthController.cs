using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;

namespace FlowOps.Controllers.System
{
    [ApiController]
    [Route("api/system")]
    public class HealthController : ControllerBase
    {
        private static readonly DateTime StartedAtUtc = DateTime.UtcNow;
        private readonly IHostEnvironment _environment;

        public HealthController(IHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpGet("ping")]
        public ActionResult<object> Ping()
        {
            return Ok(new
            {
                status = "ok",
                timestamp = DateTime.UtcNow
            });
        }

        [HttpGet("uptime")]
        public ActionResult<object> Uptime()
        {
            var uptime = DateTime.UtcNow - StartedAtUtc;
            return Ok(new
            {
                startedAtUtc = StartedAtUtc,
                uptime = uptime,
                uptimeSeconds = uptime.TotalSeconds
            });
        }

        [HttpGet("info")]
        public ActionResult<object> Info()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
            return Ok(new
            {
                environment = _environment.EnvironmentName,
                machine = Environment.MachineName,
                version,
                processId = Environment.ProcessId
            });
        }
    }
}