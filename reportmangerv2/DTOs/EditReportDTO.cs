using System;
using System.ComponentModel.DataAnnotations;
using reportmangerv2.Enums;

namespace reportmangerv2.DTOs;

public class EditReportDTO
{
    public required string Name { get; set; }
    public  string? ReportDescription { get; set; }



    public required string  ReportQuery { get; set; } 
    public required string SchemaId { get; set; }
    public required string CategoryId { get; set; }
    public List<EditReportParameterDTO> ReportParameters { get; set; } = new();

}
