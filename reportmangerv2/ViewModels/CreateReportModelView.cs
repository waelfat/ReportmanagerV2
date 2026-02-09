using System;
using System.ComponentModel.DataAnnotations;

namespace reportmangerv2.ViewModels;

public class CreateReportModelView
{
    public string Name { get; set; }
    public string? Description { get; set; }
    public string ReportQuery { get; set; }
    public string SchemaId {get;set;}
    public string CategoryId { get; set; }
    public IList<CreateReportParameterViewModel> Parameters { get; set; } = new List<CreateReportParameterViewModel>(); 
    public bool IsActive { get; set; } = true;

}
public class CreateReportParameterViewModel
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public required string Type { get; set; }
    public bool IsRequired { get; set; }=true;
    public string? DefaultValue { get; set; }
    [Range(1,int.MaxValue)]
    public int Position { get; set; }
    public string ViewControl { get; set; } = "TextBox";
    
}
