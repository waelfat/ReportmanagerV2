using System;
using Org.BouncyCastle.Ocsp;

namespace reportmangerv2.Events;

public class EventPublisher:IEventPublisher
{
    private readonly ILogger<EventPublisher> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    public EventPublisher(ILogger<EventPublisher> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }
    public async Task PublishAsync<TEvent>(TEvent eventToPublish, CancellationToken cancellationToken = default) 
    {
        // Here you would implement the logic to publish the event, e.g., to a message queue, event bus, etc.
        // For demonstration purposes, we'll just write the event details to the console.
        var handlers=_scopeFactory.CreateScope().ServiceProvider.GetServices<IEventHandler<TEvent>>();
        foreach (var handler in handlers)
        {
            _logger.LogInformation($"Publishing event: {eventToPublish?.GetType().Name}");
            await handler.HandleAsync(eventToPublish, cancellationToken);
        }
       
    }
    public async Task PublishAsync<TEvent>(IEnumerable<TEvent> eventsToPublish, CancellationToken cancellationToken = default)
    {
        foreach (var eventToPublish in eventsToPublish)
        {
            await PublishAsync(eventToPublish, cancellationToken);
        }
    }


}
