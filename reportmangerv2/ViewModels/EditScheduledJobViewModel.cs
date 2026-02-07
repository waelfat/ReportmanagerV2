using reportmangerv2.Domain;

namespace reportmangerv2.ViewModels;

public class EditScheduledJobViewModel
{
    public required string Id { get; set; } 
    public string? Description { get; set; }
    public required string CronExpression { get; set; } 
    public required string SchemaId {get;set;}
    public required string ProcedureName { get; set; }
    public bool IsActive{ get; set; }=true;
    public List<EditScheduledJobParameter> Parameters { get; set; } = new();
    public string MessageSubject { get; set; }
    public string MessageBody { get; set; }
    public string SendToEmails { get; set; }
    public string CCMails { get; set; }
}

public class EditScheduledJobParameter
{
    public required string Name { get; set; }
    public  string?  Value { get; set; }
    public required string Type  { get; set; }
    public required string Direction { get; set; }
}