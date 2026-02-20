using reportmangerv2.Enums;

namespace reportmangerv2.Events;

public class ExecutionCompletedEvent
{
    public required string ExecutionId { get; set; }
    public DateTime CompletedOn { get; set; }=DateTime.Now;
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
    public string UserId {get;set;}
    public string? ResultFilePaths { get; set; }
    public ExecutionStatus ExecutionStatus { get; set; }
    
}
