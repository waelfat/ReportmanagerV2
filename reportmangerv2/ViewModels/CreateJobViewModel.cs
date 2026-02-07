using System;
using System.Data;
using Microsoft.Build.Framework;
using Oracle.ManagedDataAccess.Client;

namespace reportmangerv2.ViewModels;

public class CreateJobViewModel
{
    
    public required string ProcedureName { get; set; }
    public string? Description { get; set; }
    public required string CronExpression { get; set; }
    public bool IsActive { get; set; }
    public string JobType { get; set; } = "StoredProcedure";


       public required string SchemaId { get; set; }
 
    public string? MessageSubject { get; set; }
    public string? MessageBody { get; set; }
    public string? SendToEmails { get; set; }
    public string? CCMails { get; set; }
    
    public List<ScheduledJobParameterModelView> Parameters { get; set; } = new List<ScheduledJobParameterModelView>();



}

public class ScheduledJobParameterModelView
{
     [Required]
    public required string Name { get; set; }
    public string? Value { get; set; }
    //public ParameterDirection f { get; set; }= ParameterDirection.Input


    public  string Type { get; set; }=OracleDbType.Varchar2.ToString();//  "varchar2";

    public required string Direction { get; set; }=ParameterDirection.Input.ToString();

}