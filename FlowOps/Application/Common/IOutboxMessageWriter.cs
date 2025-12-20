using FlowOps.BuildingBlocks.Integration;

namespace FlowOps.Application.Common
{
    public interface IOutboxMessageWriter
    {
        Task AddAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken = default);
    }
}
