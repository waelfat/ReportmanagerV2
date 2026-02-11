using System.ComponentModel.DataAnnotations;

namespace reportmangerv2.ViewModels;

public class ReportViewModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string SchemaName { get; set; }
    public string CategoryName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedByUserName { get; set; }
}

public class EditReportViewModel
{
    [Required]
    public string Id { get; set; }
    [Required]
    public string Name { get; set; }
    public string? Description { get; set; }
    [Required]
    public string ReportQuery { get; set; }
    [Required]
    public string SchemaId { get; set; }
    [Required]
    public string CategoryId { get; set; }
    public bool IsActive { get; set; }
    public List<EditReportParameterViewModel> Parameters { get; set; } = new();
}

public class EditReportParameterViewModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string Type { get; set; }
    public string ViewControl { get; set; }
    public string? DefaultValue { get; set; }
    public int Position { get; set; }
    public bool IsRequired { get; set; }
}

public class DeleteReportViewModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string SchemaName { get; set; }
    public string CategoryName { get; set; }
    public int ParametersCount { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedByUserName { get; set; }
}
