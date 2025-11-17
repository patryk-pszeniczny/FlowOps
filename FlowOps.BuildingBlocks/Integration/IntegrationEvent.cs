using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowOps.BuildingBlocks.Integration
{
    public abstract class IntegrationEvent
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public DateTime OccuredOn { get; init; } = DateTime.UtcNow;
        public int Version { get; init; } = 1;
    }
}
