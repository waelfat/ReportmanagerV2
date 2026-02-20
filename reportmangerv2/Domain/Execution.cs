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
    // public bool IsScheduledReport=> ScheduledJobId!=null && Report !=null;
    // public bool IsReport => ReportId!=null && ScheduledJobId==null;
    // public bool IsScheduledJob=> ScheduledJobId!=null && Report ==null;
    public void SetSuccess(string ResultFilePath){
        
        this.ExecutionStatus=ExecutionStatus.Completed;
        this.ErrorMessage=string.Empty;
        this.Duration=DateTime.Now-ExecutionDate;
        this.ResultFilePath=ResultFilePath;
        switch (ExecutionType)
        {
            case ExecutionType.Report:
                //do nothing for now
                break;
            case ExecutionType.Job:
                this.ScheduledJob!.JobStatus=ExecutionStatus.Pending;
                this.ScheduledJob!.SetNextRunTime();
                break;
            case ExecutionType.ScheduledQuery:
                this.ScheduledJob!.JobStatus=ExecutionStatus.Completed;
                this.ScheduledJob!.IsActive=false;
                break;
            default:
                break;
        }
        // if(IsScheduledReport)
        // {
        //     ScheduledJob!.JobStatus=ExecutionStatus.Completed;
        //     ScheduledJob!.IsActive=false;
        // }else if(IsScheduledJob){
        //     this.ScheduledJob!.JobStatus=ExecutionStatus.Pending;
        //     this.ScheduledJob!.SetNextRunTime();
        // }
        // else if(IsReport)
        // {
        
        // //do nothing for now

        // }
    }
    public void SetFailed(string ErrorMessage){
        this.ExecutionStatus=ExecutionStatus.Failed;
        this.ErrorMessage=ErrorMessage;
        this.Duration=DateTime.Now-ExecutionDate;
        switch (this.ExecutionType)
        {
            case ExecutionType.Report:
                //do nothing for now
                break;
            case ExecutionType.Job:
                this.ScheduledJob!.JobStatus=ExecutionStatus.Failed;
                this.ScheduledJob!.IsActive=false;
                break;
            case ExecutionType.ScheduledQuery:
                this.ScheduledJob!.JobStatus=ExecutionStatus.Failed;
                this.ScheduledJob!.IsActive=false;
                break;
            default:
                break;
                
        }

        // if(IsScheduledReport){
        //     ScheduledJob!.JobStatus=ExecutionStatus.Failed;
        //     ScheduledJob!.IsActive=false;
            
        // }
        // else if(IsScheduledJob)
        // {
        //     this.ScheduledJob!.JobStatus=ExecutionStatus.Pending;
        //     //calculate next run time
        //     this.ScheduledJob!.SetNextRunTime();
        // }else if (IsReport)
        // {
        //     this.ExecutionStatus=ExecutionStatus.Failed;
        //     this.ErrorMessage=ErrorMessage;
        // }
    }



}
