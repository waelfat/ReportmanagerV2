using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using reportmangerv2.Data;
using reportmangerv2.ViewModels;
using reportmangerv2.Domain;
using reportmangerv2.Enums;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Rendering;
using reportmangerv2.Services;
using Oracle.ManagedDataAccess.Client;

namespace reportmangerv2.Controllers;

public class ReportController : Controller
{
    private readonly AppDbContext _context;
    private readonly ReportService _reportService;
    private readonly ILogger<ReportController> _logger;
    public ReportController(AppDbContext context, ILogger<ReportController> logger, ReportService reportService)
    {
        _context = context;
        _logger = logger;
        _reportService = reportService;

    }
    [HttpGet]
    public async Task<IActionResult> create()
    {
        var availableSchemasSelectListItems = await _context.Schemas.Select(s => new SelectListItem { Text = s.Name, Value = s.Id }).ToListAsync();
        ViewBag.AvailableSchemas = availableSchemasSelectListItems;
        var categoriesHierarchyQuery = @"
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
        try
        {
            

        var categroiesHierarchy =await _context.Categories.FromSqlRaw(categoriesHierarchyQuery).Select(c => new SelectListItem { Text = c.Name, Value = c.Id.ToString() }).ToListAsync();

        ViewBag.CategoriesHierarchy = categroiesHierarchy;



        }
        catch(Exception ex)
        {
            _logger.Log(LogLevel.Error,"error in fetching categories hierarchy {ex}" ,ex.Message);
            return BadRequest("error in fetching categories");

             
        }
        return View();

    }
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReportModelView model)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
            return BadRequest(string.Join(", ", errors));
        }

        try
        {
            var report = new Report
            {
                Id = Guid.NewGuid().ToString(),
                Name = model.Name,
                Description = model.Description,
                CreatedById = User.FindFirstValue(ClaimTypes.NameIdentifier),
                CreatedDate = DateTime.UtcNow,
                SchemaId = model.SchemaId,
                ReportQuery = model.ReportQuery,
                IsActive = model.IsActive,
                CategoryId = model.CategoryId,
                ReportParameters = model.Parameters.Select(p => new ReportParameter
                {
                    Name = p.Name,
                    Description = p.Description,
                    Type = Enum.Parse<OracleDbType>(p.Type, true),
                    ViewControl = Enum.Parse<ViewControl>(p.ViewControl, true),
                    DefaultValue = p.DefaultValue,
                    Position = p.Position,
                    IsRequired = p.IsRequired
                }).ToList()
            };

            await _context.Reports.AddAsync(report);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Report created successfully with ID: {ReportId}", report.Id);
            return Ok(new { message = "Report created successfully", reportId = report.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating report");
            return StatusCode(500, "An error occurred while creating the report");
        }
    }
    /*
    
    we must spcify a category in report creation but categories are stored in hierarchy table categories so in create view we will create 
    */

    public async Task<IActionResult> GetReportById(string id)
    {
        var report = await _context.Reports.FindAsync(id);

        if (report == null)
        {
            _logger.LogWarning($"Report with ID {id} not found");
            return NotFound();
        }
        ExecuteReportViewModel rpt = new ExecuteReportViewModel
        {
            Id = report.Id,
            Name = report.Name,
            Description = report.Description,
            Parameters = report.ReportParameters.Select(p => new ParameterViewModel { Name = p.Name, Value = p.DefaultValue ?? "" }).ToList()

        };

        // Load previous executions for this report
        var executions = await _context.Executions
            .Where(e => e.ReportId == report.Id)
            .Include(e => e.User)
            .OrderByDescending(e => e.ExecutionDate)
            .Select(e => new reportmangerv2.ViewModels.ExecutionSummaryViewModel
            {
                Id = e.Id,
                ExecutionDate = e.ExecutionDate,
                Status = e.ExecutionStatus.ToString(),
                User = e.User != null ? e.User.UserName : null,
                ResultFilePath = e.ResultFilePath,
                Duration = e.Duration
            }).ToListAsync();

        rpt.Executions = executions;

        _logger.LogInformation($"Report with ID {id} retrieved successfully");
        return PartialView("_ExecuteReport", rpt);
    }

    [HttpGet]
    public async Task<IActionResult> DownloadExecutionResult(string id)
    {
        var exec = await _context.Executions.FindAsync(id);
        if (exec == null) return NotFound();
        if (string.IsNullOrEmpty(exec.ResultFilePath) || !System.IO.File.Exists(exec.ResultFilePath)) return NotFound();
        var provider = new Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider();
        var contentType = provider.TryGetContentType(exec.ResultFilePath, out var ct) ? ct : "application/octet-stream";
        return PhysicalFile(exec.ResultFilePath, contentType,
         Path.GetFileName(exec.ResultFilePath).Contains("_") ? Path.GetFileName(exec.ResultFilePath).Split("_")[0] :
 Path.GetFileName(exec.ResultFilePath));
    }

}
