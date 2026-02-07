using System;

namespace reportmangerv2.ViewModels;

public class ScheduledJobViewModel
{
    public string Id { get; set; }
    public string? Description { get; set; }
    public string Schema { get; set; }
    public string ProcedureName { get; set; }
    public string CronExpression { get; set; }
    public DateTime? NextRunAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public bool IsActive { get; set; }


}
