using Microsoft.EntityFrameworkCore;
using ReportManagerv2.Services;
using reportmangerv2.Data;
using reportmangerv2.Enums;
using reportmangerv2.Services;

namespace reportmangerv2.Events;

public class ExecutionCompletedEventHandler : IEventHandler<ExecutionCompletedEvent>
{
    private readonly ILogger<ExecutionCompletedEventHandler> _logger;
    private readonly IExecutionNotificationService _notify;
    private readonly AppDbContext _context;
    private readonly CurrentActiveExecutionsService _currentActiveExecutionservice;
    public ExecutionCompletedEventHandler(ILogger<ExecutionCompletedEventHandler> logger,
    AppDbContext context,
    IExecutionNotificationService notify,
    CurrentActiveExecutionsService currentActiveExecutionsService)
    {
        _logger = logger;
        _context = context;
        _currentActiveExecutionservice = currentActiveExecutionsService;
        _notify = notify;
    }

    public async Task HandleAsync(ExecutionCompletedEvent @event, CancellationToken cancellationToken = default)
    {
        
        _logger.LogInformation($"Handling completion event for ExecutionId: {@event.ExecutionId}");
        // Here you would implement the logic to handle the completion, e.g., update database records, notify users, etc.
        var execution=await _context.Executions
        .Include(e=>e.Report).Include(e=>e.ScheduledJob)
        .FirstOrDefaultAsync(e=>e.Id==@event.ExecutionId);
        if (execution!=null)
        {
            if (@event.ExecutionStatus==ExecutionStatus.Failed)
            {
                execution.SetFailed(@event.ErrorMessage!);
            }
            else
            {
                
           execution.SetSuccess(@event.ResultFilePaths!);

                

            }
    
             await _context.SaveChangesAsync(cancellationToken);
              _currentActiveExecutionservice.RemoveExecution(execution.Id);
             _logger.LogInformation($"Execution completed and saved to db with id: {execution.Id}");

            await _notify.NotifyExecutionCompleted(execution.Id, @event.ExecutionStatus==ExecutionStatus.Failed?"Failed": "Succeeded",(int)execution.Duration.TotalSeconds,execution.ResultFilePath!=null ? true:false,execution.UserId!);
        }
      
    }
}