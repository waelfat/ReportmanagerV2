using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Channels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReportManagerv2.Services;
using reportmangerv2.Data;
using reportmangerv2.Domain;
using reportmangerv2.Enums;
using reportmangerv2.Models;
using reportmangerv2.ViewModels;

namespace reportmangerv2.Controllers;

[Authorize]
public class HomeController : Controller
{
    // fake context list for reports and reportparameters
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    private readonly CurrentActiveExecutionsService _currentActiveExecutionsService;
    private readonly Channel<ExecutionRequest> _executionChannel;


    public HomeController(ILogger<HomeController> logger, AppDbContext context, UserManager<ApplicationUser> userManager,
    CurrentActiveExecutionsService currentActiveExecutionsService,
    Channel<ExecutionRequest> executionChannel)
    {

        _logger = logger;
        _context = context;
        _userManager = userManager;
        _currentActiveExecutionsService = currentActiveExecutionsService;
        _executionChannel = executionChannel;
    }
    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> ExecuteReport(string id)
    {
        //   var report = await _context.Reports.Include(r=>r.ReportParameters).Include(r=>r.Executions ).Where(r=>r.Id==id ).FirstOrDefaultAsync();
        var reportwithExecutions = await _context.Reports.Include(r => r.ReportParameters).Where(r => r.Id == id).Select(r => new
        {
            r.Id,
            r.Name,
            r.Description,
            Executions = r.Executions.Where(e => e.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier)).OrderByDescending(e => e.ExecutionDate).Take(10),
            ReportParameters = r.ReportParameters.Select(rp => new { rp.Name, rp.DefaultValue, rp.ViewControl })
        }).FirstOrDefaultAsync();

        if (reportwithExecutions == null)
        {
            _logger.LogWarning($"Report with ID {id} not found");
            return NotFound();
        }
        ExecuteReportViewModel rpt = new ExecuteReportViewModel
        {
            Id = reportwithExecutions.Id,
            Name = reportwithExecutions.Name,
            Description = reportwithExecutions.Description,
            Parameters = reportwithExecutions.ReportParameters.Select(p => new ParameterViewModel { Name = p.Name, Value = p.DefaultValue ?? "", ViewControl = p.ViewControl }).ToList()

        };


        // var users=_context.Users.ToList();
        // foreach (var usr in users)
        // {
        //     var fullNameClaim = await _userManager.GetClaimsAsync(usr);
        //     if (!fullNameClaim.Any(c => c.Type == "FullName"))
        //     {
        //         await _userManager.AddClaimAsync(usr, new Claim("FullName", usr.FullName));
        //     }
        // }
        //get fullname from claims
        ViewBag.FullName = User.FindFirstValue("FullName");
        ViewBag.UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        
        // Get active executions for this user
        var userActiveExecutions = reportwithExecutions.Executions
            .Where(e => (e.ExecutionStatus == ExecutionStatus.Running || e.ExecutionStatus == ExecutionStatus.Pending) && 
                       _currentActiveExecutionsService.IsExecutionActive(e.Id))
            .Select(e => e.Id)
            .ToList();
        ViewBag.ActiveExecutions = userActiveExecutions;
        var reportExecutions = reportwithExecutions.Executions.Where(e => e.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier)).Select(e => new ExecutionSummaryViewModel
        {

            Id = e.Id,
            ExecutionDate = e.ExecutionDate,
            Status = e.ExecutionStatus.ToString(),
            Duration = e.Duration,
            ResultFilePath = e.ResultFilePath,
            User = e.UserId



        }).ToList();
        //ensure pending and running executions are in active executions otherwise make them failed
        foreach (var execution in reportExecutions.Where(e => e.Status == "Pending" || e.Status == "Running"))
        {
            if (!_currentActiveExecutionsService.IsExecutionActive(execution.Id))
            {
                var executionEntity = await _context.Executions.FindAsync(execution.Id);
                if (executionEntity != null)
                {
                    //if execution has fileresult set it completed otherwise make it failed
                    if (!string.IsNullOrEmpty(executionEntity.ResultFilePath) && System.IO.File.Exists(executionEntity.ResultFilePath) )
                    {
                        //if the result file exists then mark as completed otherwise failed
                        
                        
                        executionEntity.ExecutionStatus = ExecutionStatus.Completed;
                        //edit in the main list reportExecutions
                        reportExecutions.First(e => e.Id == execution.Id).Status = "Completed";

                        
                     
                    }else
                    {
                        executionEntity.ExecutionStatus = ExecutionStatus.Failed;
                        reportExecutions.First(e=>e.Id==execution.Id).Status="Failed";
                        
                    }
                    await _context.SaveChangesAsync();
                }
            }
        }
        rpt.Executions = reportExecutions;
        _logger.LogInformation($"Report with ID {id} retrieved successfully");
        return PartialView("_ExecuteReport", rpt);
    }

    [HttpPost]

    public async Task<IActionResult> Execute(ExecuteReportViewModel model)
    {
        if (string.IsNullOrEmpty(model?.Id))
        {
            return BadRequest(new { error = "Report id is required" });
        }

        // var report = await _context.Reports.Include(r=>r.Schema).Include(r=>r.ReportParameters).FirstOrDefaultAsync(r=>r.Id==model.Id);
        var report = await _context.Reports.Where(r => r.Id == model.Id).Select(r => new { r.Id, r.Name, r.Schema.ConnectionString, r.ReportQuery, parameters = r.ReportParameters.Select(p => new { p.Name, p.DefaultValue }) }).FirstOrDefaultAsync();
        if (report == null)
        {
            return NotFound(new { error = "Report not found" });
        }

        var exec = new Execution
        {
            ReportId = report.Id,
            ExecutionStatus = ExecutionStatus.Pending,
            ExecutionType = ExecutionType.Report,
            // get id of the user
            UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous",


            ExecutionParameters = model.Parameters.Select(p => new ExecutionParameter { Name = p.Name, Value = p.Value }).ToList()
        };

        _context.Executions.Add(exec);
        await _context.SaveChangesAsync();
        try
        {
            await _executionChannel.Writer.WriteAsync(new ExecutionRequest
            {
                Id = report.Id,
                ConnectionString = report.ConnectionString,
                ReportTitle = report.Name,

                SQLStatement = report.ReportQuery,
                UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous",
                Type = ExecutionRequesType.SqlStatement,

                ExecutionId = exec.Id
            });

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error executing report {report.Id}");
            exec.ExecutionStatus = ExecutionStatus.Failed;
            await _context.SaveChangesAsync();
            return StatusCode(500, new { error = "Internal server error" });
        }

        _logger.LogInformation($"Execution {exec.Id} created for report {report.Id}");
        return Json(new { success = true, executionId = exec.Id });
    }

    [HttpPost]
    public async Task<IActionResult> CancelExecution(string executionId)
    {
        if (string.IsNullOrEmpty(executionId))
        {
            return BadRequest(new { error = "Execution ID is required" });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var execution = await _context.Executions.FirstOrDefaultAsync(e => e.Id == executionId && e.UserId == userId);
        
        if (execution == null)
        {
            return NotFound(new { error = "Execution not found" });
        }

        if (execution.ExecutionStatus != ExecutionStatus.Running && execution.ExecutionStatus != ExecutionStatus.Pending)
        {
            return BadRequest(new { error = "Execution cannot be cancelled" });
        }

        // Cancel the execution in the service
        _currentActiveExecutionsService.CancelExecution(executionId);
        
        _logger.LogInformation($"Execution {executionId} cancelled by user {userId}");
        return Json(new { success = true });
    }
    [HttpPost]
    public async Task<IActionResult> DeleteExecution(string executionId)
    {
        if (string.IsNullOrEmpty(executionId))
        {
            return BadRequest(new { error = "Execution ID is required" });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var execution = await _context.Executions.FirstOrDefaultAsync(e => e.Id == executionId && e.UserId == userId);

        if (execution == null)
        {
            return NotFound(new { error = "Execution not found" });
        }

        // Delete the execution
        _context.Executions.Remove(execution);
        await _context.SaveChangesAsync();

        // Delete the result file if it exists
          if (!string.IsNullOrEmpty(execution.ResultFilePath) && System.IO.File.Exists(execution.ResultFilePath))
        {
        try
        {
            
              
            System.IO.File.Delete(execution.ResultFilePath);
        }catch(Exception ex)
        {
            _logger.LogError(ex, $"Error deleting file {execution.ResultFilePath}");
        }
        }
    

        _logger.LogInformation($"Execution {executionId} deleted by user {userId}");
        return Json(new { success = true });
    }


}
