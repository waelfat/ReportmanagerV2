using Microsoft.EntityFrameworkCore;
using ReportManagerv2.Services;
using reportmangerv2.Data;
using reportmangerv2.Enums;
using reportmangerv2.Services;

namespace reportmangerv2.Events;

public class ExecutionCancelEventHandler : IEventHandler<ExecutionCancelEvent>
{
    private readonly ILogger<ExecutionCancelEventHandler> _logger;
    private readonly IExecutionNotificationService _notify;
    private readonly AppDbContext _context;
    private readonly CurrentActiveExecutionsService _currentActiveExecutionservice;
    public ExecutionCancelEventHandler(ILogger<ExecutionCancelEventHandler> logger,
    AppDbContext context,
    IExecutionNotificationService notify,
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
      
            
            
          await _context.SaveChangesAsync(cancellationToken);
          
       }
        
    }
}
