using System.Data;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Channels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NCrontab;
using Oracle.ManagedDataAccess.Client;
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
            ReportParameters = r.ReportParameters.Select(rp => new { rp.Name, rp.DefaultValue, rp.ViewControl,rp.IsRequired })
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
            Parameters = reportwithExecutions.ReportParameters.Select(p => new ParameterViewModel { Name = p.Name, Value = p.DefaultValue ?? "", ViewControl = p.ViewControl,IsRequired=p.IsRequired }).ToList()

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
        //ensure required parameters are provieded
        var missingParameters = new List<string>();
        foreach (var param in report.parameters)
        {
            var providedParam = model.Parameters.FirstOrDefault(p => p.Name == param.Name);
            if (param.DefaultValue == null && (providedParam == null || string.IsNullOrEmpty(providedParam.Value)))
            {
                missingParameters.Add(param.Name);
            }
        }
        if (missingParameters.Any())
        {
            return BadRequest(new { error = $"Missing required parameters: {string.Join(", ", missingParameters)}" });
        }
       
        //check if the report is already executing

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
    public async Task<IActionResult> ScheduleReport([FromBody] ScheduleReportViewModel model)
    {
        //check if the model is not valid
        if (!ModelState.IsValid)
        {
            return BadRequest(new { error = "Invalid model state" });
        }
        //check if selected date is provided
        if (model.SelectedDate == null)
        {
            return BadRequest(new { error = "scheduled date is required" });
        }
        if (string.IsNullOrEmpty(model?.ReportId) || string.IsNullOrEmpty(model?.CronExpression))
        {
            return BadRequest(new { error = "Report ID and cron expression are required" });
        }

        var report = await _context.Reports.Where(r => r.Id == model.ReportId)
            .Select(r => new { r.Id, r.Name, r.SchemaId, r.ReportQuery })
            .FirstOrDefaultAsync();
            
        if (report == null)
        {
            return NotFound(new { error = "Report not found" });
        }

        var job = new ScheduledJob
        {
            ProcedureName = report.Name,
            
            JobType = JobType.SqlStatement,
            CronExpression = model.CronExpression,
            IsActive = true,
            CreatedById = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "Anonymous",
            Description = $"Scheduled job for report {report.Name}",
            JobStatus = ExecutionStatus.Pending,
           // NextRunAt = CrontabSchedule.Parse(model.CronExpression).GetNextOccurrence(DateTime.Now.AddMinutes(2)),
           //assing nextrunat to selecteddate
            NextRunAt = model.SelectedDate,// > DateTime.Now ? model.SelectedDate : CrontabSchedule.Parse(model.CronExpression).GetNextOccurrence(DateTime.Now.AddMinutes(2)),
        
            SchemaId = report.SchemaId,
            SqlStatement = report.ReportQuery,
            Parameters = model.Parameters?.Select(p => new ScheduledJobParameter
            {
                Name = p.Name,
                Value = p.Value,
                Direction = ParameterDirection.Input,
                Type = OracleDbType.Varchar2
            }).ToList() ?? new List<ScheduledJobParameter>()
        };
         var exec = new Execution
        {
            ReportId = report.Id,
            ExecutionStatus = ExecutionStatus.Scheduled,
            ExecutionType = ExecutionType.ScheduledQuery,
            ScheduledJobId = job.Id,
            ExecutionDate=model.SelectedDate ?? DateTime.Now,
            // get id of the user
            UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous",


            ExecutionParameters = model.Parameters.Select(p => new ExecutionParameter { Name = p.Name, Value = p.Value }).ToList()
        };
        _context.Executions.Add(exec);

        _context.Add(job);
        
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Report {model.ReportId} scheduled with cron expression {model.CronExpression}");
        return Json(new { executionId= exec.Id });
    }
    [HttpPost]
    public async Task<IActionResult> CancelExecution(string executionId)
    {
 
        if (string.IsNullOrEmpty(executionId))
        {
            return BadRequest(new { error = "Execution ID is required" });
        }

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var execution = await _context.Executions.Include(e=>e.ScheduledJob).FirstOrDefaultAsync (e => e.Id == executionId );
        
        if (execution == null)
        {
            return NotFound(new { error = "Execution not found" });
        }
        //check if the execution is scheduled 
       if(execution.ExecutionType==ExecutionType.ScheduledQuery)
       {
         return await CancelScheduled(execution);
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
     private async Task<IActionResult> CancelScheduled(Execution execution)
    {
        //get job from executionid
       
       
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      //  var execution = await _context.Executions.Include(e=>e.ScheduledJob).FirstOrDefaultAsync(e => e.Id == executionId) ;
        
        if (execution == null)
        {
            return NotFound(new { error = "Execution not found" });
        }
      
        if (execution.ScheduledJob == null)
        {
            return NotFound(new { error = "Scheduled job not found" });
        }
        // Cancel the scheduled job 
        // if user isnot the creator or is not admin
        if (!(User.IsInRole("Admin") || execution.ScheduledJob.CreatedById != userId))
        {
            return Forbid();
        }
        if (execution.ExecutionStatus==ExecutionStatus.Completed || execution.ExecutionStatus == ExecutionStatus.Failed)
        {
            return BadRequest(new { error = "Scheduled execution finished "});
        }
        execution.ScheduledJob.IsActive = false;
        execution.ScheduledJob.JobStatus=ExecutionStatus.Cancelled;
        execution.ExecutionStatus = ExecutionStatus.Cancelled;

        await _context.SaveChangesAsync();
        //check if the exection is currently running
        if (_currentActiveExecutionsService.IsExecutionActive(execution.Id))
        {
            _currentActiveExecutionsService.CancelExecution(execution.Id);
        }
        
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
        var execution = await _context.Executions.Include(e=>e.ScheduledJob).FirstOrDefaultAsync(e => e.Id == executionId && e.UserId == userId);

        if (execution == null)
        {
            return NotFound(new { error = "Execution not found" });
        }

        // Delete the execution
        if (execution.ExecutionStatus == ExecutionStatus.Running)
        {
            return BadRequest(new { error = "Running executions cannot be deleted" });
        }
        if (execution.ExecutionType == ExecutionType.ScheduledQuery)
        {
            if (execution.ScheduledJob != null && execution.ScheduledJob.CreatedById != userId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }
            if (execution.ScheduledJob != null)
            {
                _context.ScheduledJobs.Remove(execution.ScheduledJob);
            }
        }
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
