using reportmangerv2.Enums;

namespace reportmangerv2.Events;

public class ExecutionStartedEvent
{
    public string ExecutionId { get; set; }
    public ExecutionType ExecutionType {get;set;}
    public string? ReportId { get; set; }
    public string? JobId { get; set; }
    public string? UserId { get; set; }
    public DateTime StartedOn { get; set; }=DateTime.Now;
    
}
