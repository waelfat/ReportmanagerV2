
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using reportmangerv2.Data;
using ReportManagerv2.Services;

namespace reportmangerv2.Controllers;

[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
	private readonly CurrentActiveExecutionsService _currentActiveExecutionsService;
	private readonly AppDbContext _context;

	public DashboardController(CurrentActiveExecutionsService currentActiveExecutionsService, AppDbContext context)
	{
		_currentActiveExecutionsService = currentActiveExecutionsService;
		_context = context;
	}

	[HttpGet]
	public async Task<IActionResult> Index()
	{
		// Get all running executions from the service
		var activeExecutions = _currentActiveExecutionsService.GetAllExecutions();
			

		var executionIds =  activeExecutions.Select(e => e.ExecutionId).ToArray();
		var executions = await _context.Executions
			.Include(e => e.User)
			.Include(e => e.Report)
			.Include(e => e.ScheduledJob)
			.Where(e => executionIds.Contains(e.Id))
			.ToListAsync();
		// var scheduledReports=
		// 	await _context.ScheduledJobs
		
		// 	.Include(s => s.CreatedBy)
		// 	.Where(s => s.NextRunAt < DateTime.UtcNow && s.IsActive)
		// 	.ToListAsync();

		return View(executions);

	}
}
