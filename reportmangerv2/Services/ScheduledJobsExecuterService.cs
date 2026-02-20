using System;
using System.Threading.Channels;

using Microsoft.EntityFrameworkCore;
using NCrontab;

using reportmangerv2.Data;
using reportmangerv2.Domain;
using reportmangerv2.Enums;
using reportmangerv2.Events;
using reportmangerv2.Services;


namespace ReportManagerv2.Services;

public class ScheduledJobsExecuterService : BackgroundService
{
    private readonly Channel<ExecutionRequest> _channel;
    private readonly ILogger<ExecutionService> _logger;
    private readonly CurrentActiveExecutionsService _currentActiveExecutionsService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IConfiguration _configuration;
    private readonly IEventPublisher _eventPublisher;
    //notifyservice


    public ScheduledJobsExecuterService(Channel<ExecutionRequest> channel, ILogger<ExecutionService> logger,
    CurrentActiveExecutionsService currentActiveExecutionsService,
    IServiceScopeFactory serviceScopeFactory,
    IWebHostEnvironment webHostEnvironment,
    IEventPublisher eventPublisher,
    IConfiguration configuration)
    {
        _channel = channel;
        _logger = logger;
        _currentActiveExecutionsService = currentActiveExecutionsService;
        _serviceScopeFactory = serviceScopeFactory;
        _webHostEnvironment = webHostEnvironment;
        _configuration = configuration;
        _eventPublisher = eventPublisher;



    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var counter = 1;
        try
        {

            while (!stoppingToken.IsCancellationRequested)
            {
                //  var message = _channel.Reader.ReadAsync(stoppingToken).AsTask().Result;

                _logger.LogInformation("start a new cycle for running Due Jobs");
                using var scope = _serviceScopeFactory.CreateScope();
                using var _context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // get notification service
                //var notifyService= scope.ServiceProvider.GetRequiredService<IExecutionNotificationService>();



                var _DueJobs = await _context.ScheduledJobs
                .Where(c => c.JobStatus != ExecutionStatus.Running && c.NextRunAt <= DateTime.Now && c.IsActive==true) 
                .Include(j => j.Schema).Include(j => j.Executions).ToListAsync();
                if (_DueJobs.Count > 0)
                {
                    _logger.LogInformation("Due Jobs Count = {duejobscount}", _DueJobs.Count);
                    foreach (var job in _DueJobs)
                    {
                        switch (job.JobType)
                        {
                            case JobType.SqlStatement:
                                _logger.LogInformation("Job {jobid} is Report Type", job.Id);
                                await ExecuteScheduledQuery(job, _context, scope, stoppingToken);
                                break;
                            case JobType.StoredProcedure:
                                _logger.LogInformation("Job {jobid} is Stored Procedure Type", job.Id);
                                await ExecuteJob(job, _context, stoppingToken);
                                break;
                        }




                        // _logger.LogInformation($"Job {job.Id} is now Running");
                        // //create execution 
                        // Execution execution=new Execution{ExecutionStatus=ExecutionStatus.Running,
                        // ScheduledJobId=job.Id,
                        // ExecutionDate=DateTime.Now,
                        // UserId=job.CreatedById,
                        // ExecutionType=ExecutionType.Job,
                        // ExecutionParameters=job.Parameters.Select(p=>new ExecutionParameter{Direction=p.Direction, Name=p.Name, Type=p.Type, Value=p.Value}).ToList()
                        // };
                        //  _context.Executions.Add(execution);
                        // var executeReportMessage=new ExecutionRequest {
                        //      Id = job.Id, UserId = job.CreatedById,ReportTitle=job.ProcedureName ,ExecutionId=execution.Id,
                        //      SQLStatement=job.ProcedureName, Type=ExecutionRequesType.StoredProcedure,ConnectionString=job.Schema.ConnectionString,
                        //      Body=job.MessageBody,
                        //      To=job.SendToEmails,Subject=job.MessageSubject
                        //      };
                        // CrontabSchedule crontabSchedule = CrontabSchedule.Parse(job.CronExpression);
                        // job.NextRunAt=crontabSchedule.GetNextOccurrence(DateTime.Now);
                        //   job.JobStatus=ExecutionStatus.Running;                    
                        // //job.JobStatus="RUNNING";                    
                        // _context.ScheduledJobs.Update(job);
                        // await _context.SaveChangesAsync(stoppingToken);
                        // await _channel.Writer.WriteAsync(executeReportMessage);

                    }
                }
                else
                {
                    _logger.LogInformation("No Due Jobs");
                }
                _logger.LogInformation("end of cycle for running Due Jobs");
                counter++;

                _logger.LogInformation($"start a new cycle for running Due Jobs {counter}");

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            _logger.LogInformation("ScheduledJobsExecuterService is stopping.");



        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {

            _logger.LogError("ScheduledJobsExecuterService is cancelled.");
        }
        catch (OperationCanceledException)
        {

            _logger.LogError("ScheduledJobsExecuterService is cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in ScheduledJobsExecuterService.");
        }


    }
    private async Task ExecuteJob(ScheduledJob job, AppDbContext _context, CancellationToken stoppingToken)
    {
        try
        {

            //create execution 

            Execution execution = new Execution
            {
                ExecutionStatus = ExecutionStatus.Running,
                ScheduledJobId = job.Id,
                ExecutionDate = DateTime.Now,
                UserId = job.CreatedById,
                ExecutionType = ExecutionType.Job,
                ExecutionParameters = job.Parameters.Select(p => new ExecutionParameter { Direction = p.Direction, Name = p.Name, Type = p.Type, Value = p.Value }).ToList()
            };
            _context.Executions.Add(execution);
            var executeReportMessage = new ExecutionRequest
            {
                Id = job.Id,
                UserId = job.CreatedById,
                ReportTitle = job.ProcedureName,
                ExecutionId = execution.Id,
                SQLStatement = job.ProcedureName,
                Type = ExecutionRequesType.StoredProcedure,
                ConnectionString = job.Schema.ConnectionString,
                Body = job.MessageBody,
                To = job.SendToEmails,
                Subject = job.MessageSubject


            };



            CrontabSchedule crontabSchedule = CrontabSchedule.Parse(job.CronExpression);
            job.NextRunAt = crontabSchedule.GetNextOccurrence(DateTime.Now);
            job.JobStatus = ExecutionStatus.Running;


            //job.JobStatus="RUNNING";

            _context.ScheduledJobs.Update(job);
            await _context.SaveChangesAsync(stoppingToken);
            await _channel.Writer.WriteAsync(executeReportMessage);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "error in executing job {jobid}", job.Id);
            job.JobStatus = ExecutionStatus.Failed;
            _context.ScheduledJobs.Update(job);
            await _context.SaveChangesAsync(stoppingToken);

        }

    }
    private async Task ExecuteScheduledQuery(ScheduledJob job, AppDbContext _context, IServiceScope scope, CancellationToken stoppingToken)
    {
        var execution = job.Executions.FirstOrDefault();
        try
        {
            //load execution
            if (execution == null)
            {
                _logger.LogError("No execution found for job {jobid}", job.Id);

                return;
            }
            var executeReportMessage = new ExecutionRequest
            {
                Id = job.Id,
                UserId = job.CreatedById,
                ReportTitle = job.ProcedureName,
                ExecutionId = execution.Id,
                SQLStatement = job.SqlStatement ?? throw new Exception("no sql statement found for job " + job.Id),
                Type = ExecutionRequesType.SqlStatement,
                ConnectionString = job.Schema.ConnectionString,
                Body = job.MessageBody,
                To = job.SendToEmails,
                Subject = job.MessageSubject
            };
            CrontabSchedule crontabSchedule = CrontabSchedule.Parse(job.CronExpression);
            job.NextRunAt = crontabSchedule.GetNextOccurrence(DateTime.Now);
            job.JobStatus = ExecutionStatus.Running;
            _context.ScheduledJobs.Update(job);
            await _context.SaveChangesAsync(stoppingToken);
            await _channel.Writer.WriteAsync(executeReportMessage);
            //get executionnotificationservce from scope
            var notifyService = scope.ServiceProvider.GetRequiredService<IExecutionNotificationService>();
            ExecutionStartedEvent executionStartedEvent = new ExecutionStartedEvent
            {
                ExecutionId = execution.Id,
                JobId = job.Id,
                ExecutionType = ExecutionType.Job,
                UserId = job.CreatedById
            };
            await _eventPublisher.PublishAsync(executionStartedEvent, stoppingToken);

        }
        catch (Exception e)
        {
            _logger.LogError(e, "error in executing job {jobid}", job.Id);
            job.JobStatus = ExecutionStatus.Failed;
            job.IsActive = false;

            job.Executions.OrderByDescending(e => e.ExecutionDate).FirstOrDefault()?.ExecutionStatus = ExecutionStatus.Failed;
            _context.ScheduledJobs.Update(job);
            await _context.SaveChangesAsync(stoppingToken);
            ExecutionCompletedEvent executionCompletedEvent = new ExecutionCompletedEvent
            {
                ExecutionId = execution!.Id,
                ErrorMessage = e.Message,
                ExecutionStatus = ExecutionStatus.Failed,
                ResultFilePaths = string.Empty,
                IsSuccessful = false,
                UserId = execution.UserId!,

            };
            await _eventPublisher.PublishAsync(executionCompletedEvent);




        }

    }
}
