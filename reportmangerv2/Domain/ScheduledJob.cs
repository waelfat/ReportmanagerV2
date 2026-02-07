using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using reportmangerv2.Data;
using reportmangerv2.Enums;

namespace reportmangerv2.Domain;

public class ScheduledJob
{
    public string Id { get; set; }=Guid.NewGuid().ToString();
    [Required]
    public string ProcedureName { get; set; }
    public string? Description { get; set; }
    public string CronExpression { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastRunAt { get; set; }
    public DateTime? NextRunAt { get; set; }
    [ForeignKey("CreatedById" )]
    public  string CreatedById { get; set; }
    public  ApplicationUser CreatedBy { get; set; }
    public List<ScheduledJobParameter> Parameters { get; set; } = new List<ScheduledJobParameter>();
    public List<Execution> Executions { get; set; } = new List<Execution>();
    [ForeignKey("SchemaId")]
    public string SchemaId { get; set; }
    public Schema Schema { get; set; }
    public ExecutionStatus JobStatus { get; set; }=ExecutionStatus.Pending;
    public string? MessageBody {get;set;}
    public string MessageSubject {get;set;}
    public string SendToEmails
    {
        get;
        set;
    } = string.Empty; 
    public string CCMails { 
        get; 
        set; 
    } = string.Empty;
  

}
