using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
   
   public HomeController(ILogger<HomeController> logger, AppDbContext context,UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
    }
    public IActionResult Index()
    {
        return View();
    }
    
    public async Task<IActionResult> ExecuteReport(string id)
    {
        var report = await _context.Reports.Include(r=>r.ReportParameters).Include(r=>r.Executions ).Where(r=>r.Id==id ).FirstOrDefaultAsync();

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
            Parameters= report.ReportParameters.Select(p => new  ParameterViewModel{Name=p.Name, Value=p.DefaultValue??"",ViewControl=p.ViewControl}).ToList()

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
        var reportExecutions = report.Executions.Where(e=>e.UserId== User.FindFirstValue(ClaimTypes.NameIdentifier)).Select(e=>new ExecutionSummaryViewModel
            {

               Id=e.Id,
                ExecutionDate=e.ExecutionDate,
                Status=e.ExecutionStatus.ToString(),
                Duration=e.Duration,
                User=e.UserId
            
                

            }).ToList();
            rpt.Executions=reportExecutions;
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

        var report = await _context.Reports.FindAsync(model.Id);
        if (report == null)
        {
            return NotFound(new { error = "Report not found" });
        }

        var exec = new Execution
        {
            ReportId = report.Id,
            ExecutionStatus = ExecutionStatus.Pending,
            ExecutionType=ExecutionType.Report,
            // get id of the user
            UserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous",
        
      
            ExecutionParameters = model.Parameters.Select(p => new ExecutionParameter { Name = p.Name, Value = p.Value }).ToList()
        };

        _context.Executions.Add(exec);
        await _context.SaveChangesAsync();

        _logger.LogInformation($"Execution {exec.Id} created for report {report.Id}");
        return Json(new { success = true, executionId = exec.Id });
    }
    

}
