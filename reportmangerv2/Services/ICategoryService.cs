using System;
using Microsoft.AspNetCore.Mvc.Rendering;
using reportmangerv2.Domain;


namespace reportmangerv2.Services;

public interface ICategoryService
{
    
    public Task CreateCategory(Category category);
    public Task<List<Category>> GetAllCategories();
    public Task<Category> GetCategoryById(string id);
    public Task UpdateCategory(Category category);
    public Task DeleteCategory(string id);
    public Task<List<SelectListItem>> GetCategoriesHierachy();
    //move a category to another parent
    //public Task MoveCategory(string id, string newParentId);


}
