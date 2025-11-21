using FlowOps.BuildingBlocks.Integration;
using FlowOps.BuildingBlocks.Messaging;
using FlowOps.Events;

namespace FlowOps.Services.Replay
{
    public sealed class EventRecorderListener : IHostedService
    {
        private readonly IEventBus _bus;
        private readonly ILogger<EventRecorderListener> _logger;
        private readonly EventRecorder _recorder;

        public EventRecorderListener(
            IEventBus bus,
            ILogger<EventRecorderListener> logger,
            EventRecorder recorder)
        {
            _bus = bus;
            _logger = logger;
            _recorder = recorder;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _bus.Subscribe<SubscriptionActivatedEvent>(Handle);
            _bus.Subscribe<SubscriptionCancelledEvent>(Handle);
            _bus.Subscribe<SubscriptionSuspendedEvent>(Handle);
            _bus.Subscribe<SubscriptionResumedEvent>(Handle);
            _bus.Subscribe<InvoiceIssuedEvent>(Handle);
            _bus.Subscribe<InvoicePaidEvent>(Handle);
            _logger.LogInformation("EventRecoderListener subscribed to integration events");
            return Task.CompletedTask;
        }
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("EventRecoderListener stopping");
            return Task.CompletedTask;
        }
        private Task Handle(SubscriptionActivatedEvent ev) => Append(ev);
        private Task Handle(SubscriptionCancelledEvent ev) => Append(ev);
        private Task Handle(InvoiceIssuedEvent ev) => Append(ev);
        private Task Handle(InvoicePaidEvent ev) => Append(ev);
        private Task Handle(SubscriptionSuspendedEvent ev) => Append(ev);
        private Task Handle(SubscriptionResumedEvent ev) => Append(ev);
        private Task Append(IntegrationEvent ev)
        {
            _recorder.Append(ev);
            _logger.LogDebug("Recorded {EventType} with Id={Id} at {At}", ev.GetType().Name, ev.Id, ev.OccurredOn);
            return Task.CompletedTask;
        }
    }
}
