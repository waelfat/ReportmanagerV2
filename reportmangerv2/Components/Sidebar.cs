using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using reportmangerv2.Data;

namespace reportmangerv2.Components;

public class Sidebar:ViewComponent
{
   
        public readonly AppDbContext _context;
        public Sidebar(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IViewComponentResult> InvokeAsync()
        {
            string sqlStatement = @"
SELECT
  LPAD(' ', 2*(LEVEL-1)) || c.name   AS CategoryTree,
  LEVEL                                      AS CategoryLevel,
  c.id as CategoryId,c.ParentCategoryId,
  r.id   as reportId                                    ,
  r.name as ReportName
FROM
  Categories c
  LEFT JOIN Reports r
    ON r.categoryid = c.id
START WITH
  c.ParentCategoryId IS NULL
CONNECT BY
  PRIOR c.id = c.ParentCategoryId
ORDER SIBLINGS BY c.name;

";
            var flatCategories =await _context.Database.SqlQueryRaw<CategoryReportHierarchy>(sqlStatement).ToListAsync();
          
               var  categories = GetCategoryHierarchies(flatCategories);
            
            return View(categories);
        }
        private IList<CategoryVM> GetCategoryHierarchies(IList<CategoryReportHierarchy> flatCategories) {
            var categoryHierarchies = flatCategories.Distinct().ToList();
            IList<CategoryVM> categories = new List<CategoryVM>();
            foreach (var category in flatCategories)
            {
                if (categories.Any(c => c.CategoryId == category.CategoryId))
                {
                    continue;
                }
                categories.Add(new CategoryVM
                {
                    CategoryId= category.CategoryId,
                    CategoryName= category.CategoryTree,
                    ParentCategoryId= category.ParentCategoryId,
                    Reports=flatCategories.Where(c => c.CategoryId == category.CategoryId && !string.IsNullOrWhiteSpace(c.ReportId) ).Select(c => new ReportVM
                    {
                        ReportId = c.ReportId,
                        ReportName = c.ReportName
                    }).ToList()
                  
                  
                });
            }
            foreach (var category in flatCategories)
            {
                if(category.ParentCategoryId !=null)
                {
                    var parentCategory = categories.FirstOrDefault(c => c.CategoryId == category.ParentCategoryId);
                    if(parentCategory != null)
                    {
                      if(parentCategory.ChildCategories.Any(c=>c.CategoryId == category.CategoryId))
                        {
                            continue;
                        }
                        var childCategory = categories.FirstOrDefault(x => x.CategoryId == category.CategoryId);
                        if (childCategory != null)
                        {
                            parentCategory.ChildCategories.Add(childCategory);
                            
                        }
                    }
                
                }
            }

            var rootCategories = categories.Where(c => c.ParentCategoryId == null).ToList();
            return rootCategories;

}
}

internal class ReportVM
{
   public string ReportDescription { get; set; } = string.Empty;
    public string? ReportId { get; set; }
    public string? ReportName { get; set; }
}

internal class CategoryVM
{
      public string CategoryId { get; set; }
  public string CategoryName { get; set; }
  public string? ParentCategoryId { get; set; }=null;
  public IList<CategoryVM> ChildCategories { get; set; }=new List<CategoryVM>();
  public IList<ReportVM> Reports { get; set; } =new List<ReportVM>();
}

internal class CategoryReportHierarchy
{
    public required string CategoryId { get; set; }
    public string? ParentCategoryId { get; set; }
    public required string CategoryTree { get; set; }
    public int CategoryLevel { get; set; }
    public string? ReportId { get; set; }
    public string? ReportName { get; set; } = string.Empty;

    public CategoryReportHierarchy() { }
    //override equals and gethashcode to compare on Id only
    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        CategoryReportHierarchy other = (CategoryReportHierarchy)obj;
        return CategoryId == other.CategoryId;
    }
    public override int GetHashCode()
    {
        return CategoryId.GetHashCode();
    }

}