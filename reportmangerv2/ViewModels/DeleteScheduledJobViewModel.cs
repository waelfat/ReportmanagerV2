using System;

namespace reportmangerv2.ViewModels;

public class DeleteScheduledJobViewModel
{
    public required string Id { get; set; }
    public string? CreatedBy {get;set;}

    public required string Schema { get; set; }
    public string? Description { get; set; }
    public required string ProcedureName { get; set; }
    public required string CronExpression { get; set; }
    
}

