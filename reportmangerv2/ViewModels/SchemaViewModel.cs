using System.ComponentModel.DataAnnotations;

namespace reportmangerv2.ViewModels;

public class SchemaViewModel
{
    public string Id { get; set; }
    [Required]
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Host { get; set; }
    public string Port { get; set; }
    public string ServiceName { get; set; }
    public string UserId { get; set; }
    public string Password { get; set; }
}

public class CreateSchemaViewModel
{
    [Required]
    public string Name { get; set; }
    public string Description { get; set; }
    [Required]
    public string Host { get; set; }
    [Required]
    public string Port { get; set; }
    [Required]
    public string ServiceName { get; set; }
    [Required]
    public string UserId { get; set; }
    [Required]
    public string Password { get; set; }
}

public class EditSchemaViewModel
{
    [Required]
    public string Id { get; set; }
    [Required]
    public string Name { get; set; }
    public string Description { get; set; }
    [Required]
    public string Host { get; set; }
    [Required]
    public string Port { get; set; }
    [Required]
    public string ServiceName { get; set; }
    [Required]
    public string UserId { get; set; }
    [Required]
    public string Password { get; set; }
}
