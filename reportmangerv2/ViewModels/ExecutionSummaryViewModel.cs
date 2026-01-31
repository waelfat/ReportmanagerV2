using System;

namespace reportmangerv2.ViewModels;

public class ExecutionSummaryViewModel
{
    public string Id { get; set; } = null!;
    public DateTime ExecutionDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? User { get; set; }
    public string? ResultFilePath { get; set; }
    public TimeSpan Duration { get; set; }
}
