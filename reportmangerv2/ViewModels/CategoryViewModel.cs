using System.ComponentModel.DataAnnotations;

namespace reportmangerv2.ViewModels;

public class CategoryViewModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string? ParentCategoryName { get; set; }
    public int ReportsCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedByUserName { get; set; }
}

public class CreateCategoryViewModel
{
    [Required]
    public string Name { get; set; }
    public string? ParentCategoryId { get; set; }
}

public class EditCategoryViewModel
{
    [Required]
    public string Id { get; set; }
    [Required]
    public string Name { get; set; }
    public string? ParentCategoryId { get; set; }
}

public class DeleteCategoryViewModel
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string? ParentCategoryName { get; set; }
    public int ReportsCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public string CreatedByUserName { get; set; }
}
