using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using reportmangerv2.Data;

namespace reportmangerv2.Domain;

public class Report
{
    [Key]

    public string Id { get; set; }=Guid.NewGuid().ToString();
    public required string Name { get; set; }
    public  string? Description { get; set; }
    public required string ReportQuery { get; set; }
    public  DateTime CreatedDate { get; set; }=DateTime.Now;
    public DateTime? ModifiedDate { get; set; }
    [ForeignKey("CreatedById")]
    public string CreatedById { get; set; }
    public  ApplicationUser CreatedBy { get; set; }
    [ForeignKey("SchemaId")]
    public string SchemaId { get; set; }
    public Schema Schema { get; set; }
    public ICollection<ReportParameter> ReportParameters { get; set; }=new List<ReportParameter>();
    public bool IsActive { get; set; } = true;
    
    [ForeignKey("CategoryId")]
    public string CategoryId { get; set; }

    public Category Category { get; set; }
    public ICollection<Execution> Executions { get; set; } = new List<Execution>();
    

}
