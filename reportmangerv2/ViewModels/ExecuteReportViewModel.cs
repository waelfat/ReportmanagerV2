using System;
using DocumentFormat.OpenXml.Wordprocessing;
using reportmangerv2.Enums;

namespace reportmangerv2.ViewModels;

public class ExecuteReportViewModel
{
    public string? Id {get;set;}
    public string? Name { get; set; }
    public string? Description { get; set; }
  
    public List<ParameterViewModel> Parameters { get; set; } = new(); 

    // Previous executions for display
    public List<ExecutionSummaryViewModel> Executions { get; set; } = new();

}

