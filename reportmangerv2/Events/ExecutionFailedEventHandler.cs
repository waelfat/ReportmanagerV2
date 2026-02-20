using System;
using Microsoft.EntityFrameworkCore;
using ReportManagerv2.Services;
using reportmangerv2.Data;
using reportmangerv2.Services;

namespace reportmangerv2.Events;

public class ExecutionFailedEventHandler : IEventHandler<ExecutionFailedEvent>
{
    
     private readonly ILogger<ExecutionCompletedEventHandler> _logger;
    private readonly IExecutionNotificationService _notify;
    private readonly AppDbContext _context;
    private readonly CurrentActiveExecutionsService _activeExecutions;
    public ExecutionFailedEventHandler(ILogger<ExecutionCompletedEventHandler> logger,
    IExecutionNotificationService notify,
    CurrentActiveExecutionsService activeExecutions, AppDbContext context)
    {
        _logger = logger;
        _notify = notify;
        _context = context;
        _activeExecutions = activeExecutions;
        
    }
    public async Task HandleAsync(ExecutionFailedEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Execution Failed");
        _logger.LogError(@event.ErrorMessage);
        var execution = await _context.Executions.Include(e=>e.ScheduledJob).FirstOrDefaultAsync(e=>e.Id==@event.ExecutionId);
        if(execution !=null){

            execution.SetFailed(@event.ErrorMessage);
            await _context.SaveChangesAsync(cancellationToken);
        }
           
        
        _activeExecutions.RemoveExecution(@event.ExecutionId);
        await _notify.NotifyReportFailed(@event.ExecutionId, @event.ErrorMessage, @event.UserId);
        
        
    }
}
