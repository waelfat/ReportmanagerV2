using System;
using System.Threading.Channels;
using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using NCrontab;
using Newtonsoft.Json;
using reportmangerv2.Data;
using reportmangerv2.Domain;
using reportmangerv2.Enums;


namespace ReportManager.Services;

public class ScheduledJobsExecuterService : BackgroundService
{
    private readonly Channel<ExecutionRequest> _channel;
    private readonly ILogger<ExecutionService> _logger;
    private readonly CurrentActiveExecutionsService _currentActiveExecutionsService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IConfiguration _configuration;
   
    public ScheduledJobsExecuterService(Channel<ExecutionRequest> channel, ILogger<ExecutionService> logger,
    CurrentActiveExecutionsService currentActiveExecutionsService,
    IServiceScopeFactory serviceScopeFactory,
    IWebHostEnvironment webHostEnvironment,
    IConfiguration configuration)
    {
        _channel = channel;
        _logger = logger;
        _currentActiveExecutionsService = currentActiveExecutionsService;
        _serviceScopeFactory = serviceScopeFactory;
        _webHostEnvironment = webHostEnvironment;
        _configuration = configuration;
        

    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var counter=1;
        while (!stoppingToken.IsCancellationRequested)
        {
          //  var message = _channel.Reader.ReadAsync(stoppingToken).AsTask().Result;
          
            _logger.LogInformation("start a new cycle for running Due Jobs");
            var scope = _serviceScopeFactory.CreateScope();
            var _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var _DueJobs=_context.ScheduledJobs.Include(j=>j.Schema).Where(x=>x.NextRunAt<=DateTime.Now).ToList();
            if (_DueJobs.Count>0)
            {
                foreach (var job in _DueJobs)
                {

                    var procName=await _context.ScheduledJobs.FirstOrDefaultAsync(x=>x.ProcedureName==job.ProcedureName);
                    if (procName is null)
                    {
                        continue;
                    }
                    if (job.JobStatus==ExecutionStatus.Running)
                    {
                        _logger.LogWarning($"Job {job.Id} is already running");
                        continue;
                    }
                    job.JobStatus=ExecutionStatus.Running;
                    await _context.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation($"Job {job.Id} is now Running");
                    //create execution 
                    Execution execution=new Execution{ExecutionStatus=ExecutionStatus.Running,
                    ScheduledJobId=job.Id,
                    ExecutionDate=DateTime.Now,
                    UserId=job.CreatedById,
                    ExecutionType=ExecutionType.Job,
                    ExecutionParameters=job.Parameters.Select(p=>new ExecutionParameter{Direction=p.Direction, Name=p.Name, Type=p.Type, Value=p.Value}).ToList()
                    };
                    await _context.Executions.AddAsync(execution,stoppingToken);
                    await _context.SaveChangesAsync(stoppingToken);
                    
                    var executeReportMessage=new ExecutionRequest {
                         Id = job.Id, UserId = job.CreatedById,ReportTitle=job.ProcedureName ,ExecutionId=execution.Id,
                         SQLStatement=job.ProcedureName, Type="PROCEDURE",ConnectionString=job.Schema.ConnectionString};
                         
                    
                    
                    CrontabSchedule crontabSchedule = CrontabSchedule.Parse(job.CronExpression);
                    job.NextRunAt=crontabSchedule.GetNextOccurrence(DateTime.Now);
                    //job.JobStatus="RUNNING";
                    
                    _context.ScheduledJobs.Update(job);

                    
                    await _context.SaveChangesAsync(stoppingToken);
                    await _channel.Writer.WriteAsync(executeReportMessage);

                }
            }else{
                _logger.LogInformation("No Due Jobs");
            }
           _logger.LogInformation("end of cycle for running Due Jobs");
           counter++;
           if (counter>10)
           {
               _logger.LogInformation("ending after 10 rounds");
              // counter=1;
              // break;
           }
           _logger.LogInformation($"start a new cycle for running Due Jobs {counter}");
        
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
        _logger.LogInformation("ScheduledJobsExecuterService is stopping.");
      
    }
}
