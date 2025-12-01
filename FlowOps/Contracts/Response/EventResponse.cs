namespace FlowOps.Contracts.Response
{
    public sealed class EventResponse
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = default!;
        public DateTime OccurredOn { get; set; }
        public int Version { get; set; }
    }
}
