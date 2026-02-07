using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using reportmangerv2.Data;
using reportmangerv2.Enums;

namespace reportmangerv2.Domain;

public class Execution
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();
    [ForeignKey("ReportId")]
    public string? ReportId { get; set; } = null!;
    public Report Report { get; set; } = null!;
    public DateTime ExecutionDate { get; set; } = DateTime.Now;
    public ExecutionType ExecutionType { get; set; }=ExecutionType.Report;
    public ExecutionStatus ExecutionStatus { get; set; } =ExecutionStatus.Pending;
    [ForeignKey("UserID")]
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; } 
    [ForeignKey("ScheduledJobId")]
    public string? ScheduledJobId { get; set; }
    public ScheduledJob? ScheduledJob { get; set; }
    public string? ResultFilePath { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }=TimeSpan.Zero;
    public List<ExecutionParameter> ExecutionParameters { get; set; } = new();



}
