using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using Oracle.ManagedDataAccess.Client;
using reportmangerv2.Enums;

namespace reportmangerv2.Domain;

public class ReportParameter
{
    [Key]
    public string Id {get;set;}=Guid.NewGuid().ToString();
    public string Name {get;set;}
    public string? Description {get;set;}
    public OracleDbType Type {get;set;}=OracleDbType.Varchar2;
    public ViewControl ViewControl {get;set;}=ViewControl.TextBox;

    public string? DefaultValue {get;set;}
    //greater than 0 data anotation
    [Range(1, int.MaxValue)]
    public int Position {get;set;}
    public bool IsRequired {get;set;}
    [ForeignKey("ReportId")]
    public string ReportId {get;set;}
    public Report Report {get;set;}
}