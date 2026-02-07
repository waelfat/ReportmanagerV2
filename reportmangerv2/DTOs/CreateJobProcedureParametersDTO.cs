using System;
using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace reportmangerv2.DTOs;

public class CreateJobProcedureParametersDTO
{
    
    public required string Name { get; set; }
    public required OracleDbType Type { get; set; }
    public ParameterDirection Direction { get; set; } = ParameterDirection.Input;

}
