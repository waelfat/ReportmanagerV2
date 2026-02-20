namespace reportmangerv2.Events;

public class ExecutionCancelEvent
{
   public string ExecutionId { get; set; }
    
   public string? Reason { get; set; }
    public DateTime OccurredOn { get; set; }=DateTime.Now;
    public string? StackTrace { get; set; }

}
