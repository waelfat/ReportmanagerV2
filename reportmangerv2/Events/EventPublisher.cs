using System;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Ocsp;
using ReportManagerv2.Services;
using reportmangerv2.Data;
using reportmangerv2.Enums;
using reportmangerv2.Services;

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
public class ExecutionCancelEvent
{
   public string ExecutionId { get; set; }
    
   public string? Reason { get; set; }
    public DateTime OccurredOn { get; set; }=DateTime.Now;
    public string? StackTrace { get; set; }

}
public class ExecutionStartedEvent
{
    public string ExecutionId { get; set; }
    public DateTime startedOn { get; set; }=DateTime.Now;
    
}
public class ExecutionCompletedEvent
{
    public required string ExecutionId { get; set; }
    public DateTime CompletedOn { get; set; }=DateTime.Now;
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public string UserId {get;set;}
    public string? ResultFilePaths { get; set; }
    
}
public class ReportGenerationStartedEvent
{
    public required string ReportId { get; set; }
    public required string ExecutionId { get; set; }
    public DateTime StartedOn { get; set; }=DateTime.Now;
}
public class ReportGenerationProgressEvent
{
    public string ExecutionId { get; set; }
    public int ProgressRowsNumber { get; set; }
}
public class ExectionCancelEventHandler : IEventHandler<ExecutionCancelEvent>
{
    private readonly ILogger<ExectionCancelEventHandler> _logger;
    private readonly ExecutionNotificationService _notify;
    private readonly AppDbContext _context;
    private readonly CurrentActiveExecutionsService _currentActiveExecutionservice;
    public ExectionCancelEventHandler(ILogger<ExectionCancelEventHandler> logger,
    AppDbContext context,
    ExecutionNotificationService notify,
    CurrentActiveExecutionsService currentActiveExecutionsService)
    {
        _logger = logger;
        _context = context;
        _currentActiveExecutionservice = currentActiveExecutionsService;
        _notify = notify;
    }
    public async Task HandleAsync(ExecutionCancelEvent @event, CancellationToken cancellationToken = default)
    {
       
        _logger.LogInformation($"Handling cancellation event for ExecutionId: {@event.ExecutionId}, Reason: {@event.Reason}");
        // Here you would implement the logic to handle the cancellation, e.g., update database records, notify users, etc.
       var execution= await _context.Executions.Include(e=>e.Report).Include(e=>e.ScheduledJob).FirstOrDefaultAsync(e=>e.Id==@event.ExecutionId,cancellationToken);
       if (execution!=null)
       {
          execution.ExecutionStatus=ExecutionStatus.Cancelled;
            if (_currentActiveExecutionservice.IsExecutionActive(@event.ExecutionId))
            {
                _currentActiveExecutionservice.CancelExecution(@event.ExecutionId);
            }
            //if execution is scheduled
            if (execution.ScheduledJob!=null && execution.ScheduledJob.JobType==JobType.SqlStatement)
            {
                execution.ScheduledJob.IsActive=false;
                execution.ScheduledJob.JobStatus=ExecutionStatus.Cancelled;
            }
            
          await _context.SaveChangesAsync(cancellationToken);
       }
        
    }
}
    
public class ExecutionCompletedEventHandler : IEventHandler<ExecutionCompletedEvent>
{
    private readonly ILogger<ExecutionCompletedEventHandler> _logger;
    private readonly ExecutionNotificationService _notify;
    private readonly AppDbContext _context;
    private readonly CurrentActiveExecutionsService _currentActiveExecutionservice;
    public ExecutionCompletedEventHandler(ILogger<ExecutionCompletedEventHandler> logger,
    AppDbContext context,
    ExecutionNotificationService notify,
    CurrentActiveExecutionsService currentActiveExecutionsService)
    {
        _logger = logger;
        _context = context;
        _currentActiveExecutionservice = currentActiveExecutionsService;
        _notify = notify;
    }

    public Task HandleAsync(ExecutionCompletedEvent @event, CancellationToken cancellationToken = default)
    {
        
        _logger.LogInformation($"Handling completion event for ExecutionId: {@event.ExecutionId}");
        // Here you would implement the logic to handle the completion, e.g., update database records, notify users, etc.
        var execution=_context.Executions
        .Include(e=>e.Report).Include(e=>e.ScheduledJob)
        .FirstOrDefault(e=>e.Id==@event.ExecutionId);
        if (execution!=null)
        {
            execution.ExecutionStatus=ExecutionStatus.Completed;
            execution.Duration= DateTime.Now - execution.ExecutionDate;
            execution.ResultFilePath=@event.ResultFilePaths;
            
            if (_currentActiveExecutionservice.IsExecutionActive(@event.ExecutionId))
            {
                _currentActiveExecutionservice.RemoveExecution(@event.ExecutionId);
            }
            //if execution is scheduled
            if (execution.ScheduledJob!=null && execution.ScheduledJob.JobType==JobType.SqlStatement)
            {
                execution.ScheduledJob.JobStatus=ExecutionStatus.Completed;
                execution.ScheduledJob.IsActive=false;

            }
            
             _context.SaveChanges();
        }
        return Task.CompletedTask;
    }
}