using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using reportmangerv2.Domain;
using reportmangerv2.Services;
using reportmangerv2.ViewModels;

namespace reportmangerv2.Controllers;

public class CategoryController:Controller
{
    private readonly ICategoryService _categoryService;
    private readonly ILogger<CategoryController> _logger;
    public CategoryController(ICategoryService categoryService, ILogger<CategoryController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var categories = await _categoryService.GetAllCategories();
        var viewModel = categories.Select(c => new CategoryViewModel
        {
            Id = c.Id,
            Name = c.Name,
            ParentCategoryName = c.ParentCategory?.Name,
            ReportsCount = c.Reports.Count,
            CreatedAt = c.CreatedAt,
            CreatedByUserName = c.CreatedBy?.UserName ?? "N/A"
        }).ToList();
        return View(viewModel);
    }
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var categories = await _categoryService.GetCategoriesHierachy();
        ViewBag.CategoriesHierarchy = categories;

    
        return View();
    }
    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryViewModel model)
    {
        if (ModelState.IsValid)
        {
            var category = new Category
            {
                Name = model.Name,
                ParentCategoryId = model.ParentCategoryId,
                CreatedById = User.FindFirstValue(ClaimTypes.NameIdentifier),
                CreatedAt = DateTime.UtcNow
            };
            await _categoryService.CreateCategory(category);
            return RedirectToAction(nameof(Index));
        }
        
        var categories = await _categoryService.GetCategoriesHierachy();
        ViewBag.CategoriesHierarchy = categories;
        return View(model);
    }
    
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var category = await _categoryService.GetCategoryById(id);
        if (category == null)
        {
            return NotFound();
        }
        
        var viewModel = new EditCategoryViewModel
        {
            Id = category.Id,
            Name = category.Name,
            ParentCategoryId = category.ParentCategoryId
        };
        
        var categories = await _categoryService.GetCategoriesHierachy();
        ViewBag.CategoriesHierarchy = categories;
        return View(viewModel);
    }
    
    [HttpPost]
    public async Task<IActionResult> Edit(EditCategoryViewModel model)
    {
        if (ModelState.IsValid)
        {
            var category = await _categoryService.GetCategoryById(model.Id);
            if (category == null)
            {
                return NotFound();
            }
            
            category.Name = model.Name;
            category.ParentCategoryId = model.ParentCategoryId;
            await _categoryService.UpdateCategory(category);
            return RedirectToAction(nameof(Index));
        }
        
        var categories = await _categoryService.GetCategoriesHierachy();
        ViewBag.CategoriesHierarchy = categories;
        return View(model);
    }
    
    [HttpGet]
    public async Task<IActionResult> Delete(string id)
    {
        var category = await _categoryService.GetCategoryById(id);
        if (category == null)
        {
            return NotFound();
        }
        
        var viewModel = new DeleteCategoryViewModel
        {
            Id = category.Id,
            Name = category.Name,
            ParentCategoryName = category.ParentCategory?.Name,
            ReportsCount = category.Reports.Count,
            CreatedAt = category.CreatedAt,
            CreatedByUserName = category.CreatedBy?.UserName ?? "N/A"
        };
        return View(viewModel);
    }
    
    [HttpPost, ActionName("Delete")]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        await _categoryService.DeleteCategory(id);
        return RedirectToAction(nameof(Index));
    }
}
