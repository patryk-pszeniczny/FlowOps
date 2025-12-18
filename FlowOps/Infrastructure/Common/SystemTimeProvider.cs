using FlowOps.Application.Common;

namespace FlowOps.Infrastructure.Common
{
    public sealed class SystemTimeProvider : ITimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
