using FlowOps.BuildingBlocks.Domain.Events;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace FlowOps.BuildingBlocks.Messaging
{
    public sealed class DomainEventDispatcher : IDomainDispatcher
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DomainEventDispatcher> _logger;
        public DomainEventDispatcher(IServiceProvider serviceProvider, ILogger<DomainEventDispatcher> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }
        public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
        {
            foreach(var domainEvent in domainEvents)
            {
                await DispatchSingleAsync(domainEvent, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        private async Task DispatchSingleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            var eventType = domainEvent.GetType();
            var handlerType = typeof(IEnumerable<>)
                .MakeGenericType(typeof(IDomainEventHandler<>)
                .MakeGenericType(eventType));
            var handlers = _serviceProvider.GetService(handlerType) as IEnumerable<object> ?? Enumerable.Empty<object>();
            foreach (var handler in handlers)
            {
                var method = handler
                    .GetType()
                    .GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync), 
                        new[] {
                            eventType,
                            typeof(CancellationToken)
                        });
                if(method is null)
                {
                    continue;
                }
                try
                {
                    _logger.LogDebug("Dispatching doamin event {EventType} to handler {HandlerType}.",
                        eventType.FullName,
                        handler.GetType().FullName);
                    var task = (Task)method.Invoke(handler, 
                        new object[] { 
                            domainEvent, 
                            cancellationToken 
                        })!;
                    await task.ConfigureAwait(false);
                }
                catch(TargetInvocationException ex) when (ex.InnerException is not null)
                {
                    _logger.LogError(ex.InnerException, "Domain event handler {HandlerType} threw an exception for event {EventType}.", 
                         handler.GetType().FullName, 
                         eventType.FullName);
                    throw ex.InnerException;
                }
            }
        }
    }
}
