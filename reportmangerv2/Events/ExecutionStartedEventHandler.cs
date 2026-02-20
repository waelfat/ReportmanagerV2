using System;
using reportmangerv2.Data;
using reportmangerv2.Services;

namespace reportmangerv2.Events;

public class ExecutionStartedEventHandler : IEventHandler<ExecutionStartedEvent>
{
     private readonly ILogger<ExecutionCompletedEventHandler> _logger;
    private readonly IExecutionNotificationService _notify;
    private readonly AppDbContext _context;

    public ExecutionStartedEventHandler(ILogger<ExecutionCompletedEventHandler> logger, IExecutionNotificationService notify, AppDbContext context)
    {
        _logger = logger;
        _notify = notify;
        _context = context;
    }


    
    public async Task HandleAsync(ExecutionStartedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Execution Started for {@event}", @event);
        
        cancellationToken.ThrowIfCancellationRequested();

        await _notify.NotifyExecutionStarted(@event.ExecutionId,@event.ReportId??@event.JobId,@event.UserId);
       
    }
}
