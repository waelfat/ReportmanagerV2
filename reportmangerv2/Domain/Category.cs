using System.ComponentModel.DataAnnotations.Schema;
using reportmangerv2.Data;

namespace reportmangerv2.Domain;

public class Category
{
    
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public List<Report> Reports { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [ForeignKey("CreatedById")]
    public string CreatedById { get; set; }
    public ApplicationUser CreatedBy { get; set; }
    [ForeignKey("ParentCategoryId")]
    public string? ParentCategoryId { get; set; }
    public Category? ParentCategory { get; set; }
    public List<Category> SubCategories { get; set; } = new();
    

}