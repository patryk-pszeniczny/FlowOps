namespace FlowOps.Contracts.Request.UserState
{
    public sealed class SaveDraftRequest
    {
        public Guid? DraftId { get; init; }
        public Guid? CustomerId { get; init; }
        public string? PlanCode { get; init; }
        public required string Payload { get; init; }
    }
}
