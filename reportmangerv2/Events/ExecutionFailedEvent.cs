using System;

namespace reportmangerv2.Events;

public class ExecutionFailedEvent
{
     public string ExecutionId { get; set; }
    public string ErrorMessage { get; set; }
    public string? ReportId { get; set; }
    public string? JobId { get; set; }

    public string UserId { get; set; }

}
