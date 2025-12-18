namespace FlowOps.Application.Common
{
    public interface ITimeProvider
    {
        DateTime UtcNow { get; }
    }
}
