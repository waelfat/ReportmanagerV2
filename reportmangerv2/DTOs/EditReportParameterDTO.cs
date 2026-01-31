using System;
using Oracle.ManagedDataAccess.Client;
using reportmangerv2.Enums;

namespace reportmangerv2.DTOs;

public class EditReportParameterDTO
{
    public required string Id { get; set; }
    public required string Name { get; set; }
    public  string? DefaultValue { get; set; } 
    public required OracleDbType Type { get; set; }=OracleDbType.Varchar2;
    public int Position { get; set; }
    
    public required bool IsRequired { get; set; }=true;
    public ViewControl ViewControl { get; set; } = ViewControl.TextBox;
    public string? Description { get; set; }
    

}
