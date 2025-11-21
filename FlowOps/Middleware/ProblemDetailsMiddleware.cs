namespace FlowOps.Middleware
{
    public sealed class ProblemDetailsMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ProblemDetailsMiddleware> _logger;
        public ProblemDetailsMiddleware(RequestDelegate next, ILogger<ProblemDetailsMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }
        public async Task Invoke(HttpContext contex)
        {
            try
            {
                await _next(contex);
            }
            catch (KeyNotFoundException ex)
            {
                await WriteProblem(contex, StatusCodes.Status404NotFound, "Not Found", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                await WriteProblem(contex, StatusCodes.Status409Conflict, "Conflict", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred while processing the request.");
                await WriteProblem(contex, StatusCodes.Status500InternalServerError, "Internal Server Error", ex.Message);
            }
        }
        private static async Task WriteProblem(HttpContext ctx, int status, string title, string detail)
        {
            ctx.Response.ContentType = "application/problem+json";
            ctx.Response.StatusCode = status;
            var body = new
            {
                type = $"https://httpstatuses.io/{status}",
                title,
                status,
                detail,
                tracId = ctx.TraceIdentifier
            };
            await ctx.Response.WriteAsJsonAsync(body);
        }
    }
}
