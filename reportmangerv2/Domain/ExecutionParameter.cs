using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace reportmangerv2.Domain;

public class ExecutionParameter
{
    public string? Name { get; set; }
    public string? Value { get; set; } 
    public ParameterDirection Direction    {get;set;}
    public OracleDbType Type { get; set; }=OracleDbType.Varchar2;
}