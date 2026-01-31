using System;
using System.ComponentModel.DataAnnotations;
using reportmangerv2.Enums;

namespace reportmangerv2.DTOs;

public class CreateReportDTO
{
    [Required]
    public required string ReportName { get; set; }
    public  string? ReportDescription { get; set; }
    [Required]
    public ReportType ReportType { get; set; }=ReportType.SQLSTATEMENT;

    public required string  ReportQuery { get; set; } 
    public required string SchemaId { get; set; }
    public required string CategoryId { get; set; }
    public List<CreateParameterDTO> ReportParameters { get; set; } = new();

}
