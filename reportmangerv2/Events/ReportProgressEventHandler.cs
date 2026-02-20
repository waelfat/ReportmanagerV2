using System;
using reportmangerv2.Services;

namespace reportmangerv2.Events;

public class ReportProgressEventHandler : IEventHandler<ReportProgressEvent>
{
       private readonly ILogger<ExecutionCompletedEventHandler> _logger;
    private readonly IExecutionNotificationService _notify;
    public ReportProgressEventHandler(ILogger<ExecutionCompletedEventHandler> logger, IExecutionNotificationService notify)
    {
        _logger = logger;
        _notify = notify;
    }
    public Task HandleAsync(ReportProgressEvent @event, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Report Progress for {@event}", @event);
        _logger.LogInformation("Notify users");
        _logger.LogInformation("ExecutionId: {@event.ExecutionId}", @event.ExecutionId);
        cancellationToken.ThrowIfCancellationRequested();
       
        return _notify.NotifyReportProgress(@event.ExecutionId,@event.ProgressRowsNumber,@event.UserId);
    }
}
