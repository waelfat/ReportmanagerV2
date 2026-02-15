using System;

namespace reportmangerv2.Events;

public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent eventToPublish,CancellationToken cancellationToken=default) ;

}

public interface IEvent
{
    Guid Id { get; }
    DateTime OccurredAt { get; }
    string EventType { get; }
    string UserId {get;}
}

public interface IEventHandler< TEvent> 
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
    
}
