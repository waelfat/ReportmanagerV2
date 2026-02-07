using System;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using reportmangerv2.Data;
using reportmangerv2.ViewModels;
using reportmangerv2.Domain;
using reportmangerv2.Enums;
using System.Security.Claims;

namespace reportmangerv2.Controllers;

public class ReportController : Controller
{
    private readonly AppDbContext _context;
    private readonly ILogger<ReportController> _logger;
    public ReportController(AppDbContext context, ILogger<ReportController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IActionResult> GetReportById(string id)
    {
        var report = await _context.Reports.FindAsync(id);

        if (report == null)
        {
            _logger.LogWarning($"Report with ID {id} not found");
            return NotFound();
        }
        ExecuteReportViewModel rpt=new ExecuteReportViewModel
        {
            Id = report.Id,
            Name = report.Name,
            Description = report.Description,
            Parameters= report.ReportParameters.Select(p => new  ParameterViewModel{Name=p.Name, Value=p.DefaultValue??""}).ToList()

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
         Path.GetFileName(exec.ResultFilePath).Contains("_")?  Path.GetFileName(exec.ResultFilePath).Split("_")[0]:
 Path.GetFileName(exec.ResultFilePath)); 
    }

}
