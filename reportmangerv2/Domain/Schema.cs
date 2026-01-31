using System.ComponentModel.DataAnnotations;
using Oracle.ManagedDataAccess.Client;

namespace reportmangerv2.Domain;

public class Schema
{
    [Key]
    public string  Id { get; set; }=Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<Report> Reports { get; set; } = new(); 
    // oracle connection items

    public string Host { get; set; } 
    public string Port { get; set; }
    public string ServiceName { get; set; }
    public string UserId { get; set; }
    public string Password { get; set; }
    public string ConnectionString => $"Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST={Host})(PORT={Port}))(CONNECT_DATA=(SERVICE_NAME={ServiceName})));User Id={UserId};Password={Password};";
    public async Task<bool> ValidateConnectionAsync()
    {
        try
        {
            using var connection = new OracleConnection(ConnectionString);
            await connection.OpenAsync();
            return true;
        }
        catch
        {
            return false;
        }
        
    }
}
