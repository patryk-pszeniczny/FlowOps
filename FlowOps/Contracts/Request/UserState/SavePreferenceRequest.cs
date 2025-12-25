namespace FlowOps.Contracts.Request.UserState
{
    public sealed class SavePreferenceRequest
    {
        public required string Key { get; init; }
        public required string Value { get; init; }
    }
}
