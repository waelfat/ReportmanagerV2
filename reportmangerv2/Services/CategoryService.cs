using System;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using reportmangerv2.Data;
using reportmangerv2.Domain;

namespace reportmangerv2.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _context;
    //logger
    private readonly ILogger<CategoryService> _logger;
    public CategoryService(AppDbContext context, ILogger<CategoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task CreateCategory(Category category)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync();
       
       
    }

    public async Task DeleteCategory(string id)
    {
        // var category=await _context.Categories.Where(c=>c.Id==id).ExecuteDeleteAsync();
        // if(category==0)
        // {
        //     throw new Exception("Catogory not found or has children");
        // }

        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id) ?? throw new Exception("Category not found");
        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();
    }

    public async Task<List<Category>> GetAllCategories()
    {
        var categories = await _context.Categories.ToListAsync();
        return categories;
        
        
    }

    public async Task<Category> GetCategoryById(string id)
    {

        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id) ?? throw new Exception("Category not found");
        return category;
    }

    public async Task<List<SelectListItem>> GetCategoriesHierachy()
    {
       var hierarchyQuery=@"
       SELECT
  LPAD('--', 4*(LEVEL-1)) || c.name   AS Name
 --, LEVEL                                      AS CategoryLevel,
  ,c.id as Id
 -- ,  c.ParentCategoryId
FROM
  Categories c
 
START WITH
  c.ParentCategoryId IS NULL
CONNECT BY
  PRIOR c.id = c.parentcategoryid

ORDER SIBLINGS BY c.name";
         var hierarchy=await _context.Categories.FromSqlRaw(hierarchyQuery).Select(c=>new SelectListItem{
Text=c.Name,Value=c.Id}).ToListAsync();
         return hierarchy;       
       
    }

    // public async Task MoveCategory(string id, string newParentId)
    // {
        
        
    // }

    public async Task UpdateCategory(Category category)
    {
        // check if the new parent is not a descendant of the category
        // if it is, throw an exception to prevent circular reference
        if (category.ParentCategoryId == category.Id)
        {
            throw new Exception("Cannot set category as its own parent.");
            
            
        }
        if (!string.IsNullOrWhiteSpace(category.ParentCategoryId))
        {
            var isDescendant = await IsDescendantEfAsync(category.Id, category.ParentCategoryId);
            if (isDescendant)
            {
                throw new Exception("Cannot move category to its own descendant.");
            }

        }
       
        _context.Categories.Update(category);
        await _context.SaveChangesAsync();
        
      
    }
   // Minimal DTO to map the scalar result

// Usage inside a service with DbContext _context
private async Task<bool> IsDescendantEfAsync(string ancestorId, string candidateId)
{
    var sql = @"
        SELECT t.id AS Id
        FROM (
            SELECT c.id
            FROM Categories c
            START WITH c.id = :ancestorId
            CONNECT BY PRIOR c.id = c.ParentCategoryId
        ) t
        WHERE t.id = :candidateId";

    var ancestorParam = new OracleParameter("ancestorId", ancestorId);
    var candidateParam = new OracleParameter("candidateId", candidateId);

    var rows = await _context.Categories
        .FromSqlRaw(sql, ancestorParam, candidateParam)
        .AsNoTracking()
        .ToListAsync();

    return rows.Count > 0;
}
#region efore
// private async Task<bool> IsDescendant2(string parentcategoryId, string candidateId)
// {
//     var category= await _context.Categories.Include(c=>c.SubCategories).Where(c=>c.Id==candidateId).FirstOrDefaultAsync();
//     if(category==null)
//     {
//         throw new Exception("Category not found");
//     }
//    return TraverseCategory(category,parentcategoryId);


    
// }
// private bool TraverseCategory(Category category, string targetId)
// {
//     if (category.Id == targetId)
//         return true;

//     foreach (var subCategory in category.SubCategories)
//     {
//         if (TraverseCategory(subCategory, targetId))
//             return true;
//     }

//     return false;
// }
#endregion
}