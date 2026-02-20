namespace reportmangerv2.Events;

public class ReportProgressEvent
{
    public required string ExecutionId { get; set; }
    public required string UserId {get;set;}
    public int? ProgressRowsNumber { get; set; }
}
