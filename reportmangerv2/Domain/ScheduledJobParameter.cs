using System.ComponentModel.DataAnnotations;
using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace reportmangerv2.Domain;

public class ScheduledJobParameter
{
    [Required]
    public required string Name { get; set; }
    public  string? Value { get; set; }
    public required OracleDbType Type { get; set; }
    public required ParameterDirection Direction { get; set; }
}