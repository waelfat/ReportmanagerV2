using System;
using System.Data;
using System.Security.Claims;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NCrontab;
using Oracle.ManagedDataAccess.Client;
using reportmangerv2.Data;
using reportmangerv2.Domain;
using reportmangerv2.DTOs;
using reportmangerv2.Migrations;
using reportmangerv2.ViewModels;

namespace reportmangerv2.Controllers;

//requiresAuthorization with role Admin
[Authorize(Roles="Admin")]
public class ScheduledJobController:Controller
{
    private readonly ILogger<ScheduledJobController> _logger;
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;
    public ScheduledJobController (ILogger<ScheduledJobController> logger, AppDbContext context, IConfiguration configuration)
    {
        _logger=logger;
        _context=context;
        _configuration=configuration;

    
    }
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var schemas = await _context.Schemas.Select(s=>new SelectListItem{
            Value=s.Id,
            Text=s.Name}).ToListAsync();
        ViewBag.Schemas=schemas;
        return View();
       
    }
    [HttpPost]
    public async Task<IActionResult>Create([FromBody] CreateJobViewModel job)
    {
        if(!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage));
            foreach(var error in errors)
            {
                _logger.LogError(error);
            }
            return BadRequest(errors);
        }
        
        // Validate emails
        if (!string.IsNullOrWhiteSpace(job.SendToEmails) && !ValidateEmailList(job.SendToEmails))
        {
            return BadRequest("Invalid email format in Send To Emails");
        }
        
        if (!string.IsNullOrWhiteSpace(job.CCMails) && !ValidateEmailList(job.CCMails))
        {
            return BadRequest("Invalid email format in CC Emails");
        }
        
        try
        {
            var scheduledJob = new ScheduledJob
            {
                Id = Guid.NewGuid().ToString(),
                Description=job.Description,
                SchemaId = job.SchemaId,
                ProcedureName = job.ProcedureName,
                CronExpression = job.CronExpression,
                CreatedAt = DateTime.UtcNow,
                // current user for createdbyid from HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                //calculate next run time based on cronexpression using ncrontab 
                NextRunAt = CrontabSchedule.Parse(job.CronExpression).GetNextOccurrence(DateTime.Now),
                CreatedById= User.FindFirstValue(ClaimTypes.NameIdentifier)?? string.Empty,
                IsActive = job.IsActive,
                MessageSubject = job.MessageSubject,
                MessageBody = job.MessageBody,
                SendToEmails = job.SendToEmails,
                CCMails = job.CCMails
            
            };
            var jobParameters=job.Parameters.Select(x=>new ScheduledJobParameter
            {
                Name=x.Name,
                Value=x.Value?.ToString()!,// is null ?  : x.Value.ToString(),
                Direction=Enum.Parse<ParameterDirection>(x.Direction,true),
                
                Type=Enum.Parse<OracleDbType >(x.Type,true)
            }).ToList();
            scheduledJob.Parameters=jobParameters;

            _context.ScheduledJobs.Add(scheduledJob);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Scheduled job created successfully with ID: {JobId}", scheduledJob.Id);
            return Ok(new {message="Scheduled Job created successfully", jobId=scheduledJob.Id});
        }
        catch(Exception ex)
        {
            _logger.LogError(ex, "Error creating scheduled job");
            
            return StatusCode(500, "An error occurred while creating the scheduled job");
        }
    }
    
    [HttpPost]
    public async Task<IActionResult> GetProcedureParameters([FromBody] GetProcedureParametersRequest request)
    {
        var parameters = new List<CreateJobProcedureParametersDTO>();
        try
        {
             if (string.IsNullOrEmpty(request.SchemaId))
            {
                return BadRequest("Schema ID is required");
            }
            
            // First, check if procedure exists in the specified schema
            var schema = await _context.Schemas.FirstOrDefaultAsync(s=>s.Id==request.SchemaId);
            if (schema == null)
            {
                return BadRequest("Schema not found");
            }
             using (var connection = new OracleConnection(schema.ConnectionString))
            {
                await connection.OpenAsync();
                
                // First check if procedure exists
                var existsQuery = "SELECT COUNT(*) FROM ALL_PROCEDURES WHERE OWNER = :owner AND OBJECT_NAME = :procedureName";
                using (var existsCommand = new OracleCommand(existsQuery, connection))
                {
                    existsCommand.Parameters.Add(":owner", schema.UserId.ToUpper());
                    existsCommand.Parameters.Add(":procedureName", request.ProcedureName.ToUpper());
                    
                    var count = Convert.ToInt32(await existsCommand.ExecuteScalarAsync());
                    if (count == 0)
                    {
                        return BadRequest($"Procedure '{request.ProcedureName}' not found in schema '{schema.Name}'");
                    }
                }
                
                // If procedure exists, get its parameters
                var query = @"
                    SELECT ARGUMENT_NAME, DATA_TYPE, IN_OUT 
                    FROM ALL_ARGUMENTS 
                    WHERE OWNER = :owner AND OBJECT_NAME = :procedureName 
                    AND ARGUMENT_NAME IS NOT NULL
                    ORDER BY POSITION";
                
                using (var command = new OracleCommand(query, connection))
                {
                    command.Parameters.Add(":owner", schema.UserId.ToUpper());
                    command.Parameters.Add(":procedureName", request.ProcedureName.ToUpper());
                    
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            var paramName = reader.GetString("ARGUMENT_NAME");
                            var dataType = reader.GetString("DATA_TYPE");
                            var direction = reader.GetString("IN_OUT");
                            
                            parameters.Add(new CreateJobProcedureParametersDTO
                            {
                                Name = paramName,
                                Type = MapOracleDataType(dataType),
                                Direction = MapParameterDirection(direction)
                            });
                        }
                    }
                }
            }
            
            _logger.LogInformation("Successfully retrieved parameters for procedure {ProcedureName} in schema {SchemaName}", request.ProcedureName, schema.Name);
            if(parameters.Count == 0)
            {
                return BadRequest("No parameters found for the specified procedure");
            }
            

            return Ok(parameters.Select(c =>new
            {
                Name=c.Name,
                Type=c.Type.ToString(),
                Direction=c.Direction.ToString()
            }).ToList());

            
           
           
        }catch(Exception ex)
        {
               _logger.LogError(ex, "Error in GetProcedureParameters");
            return BadRequest($"Error retrieving procedure parameters: {ex.Message}");
        }
    }
     private OracleDbType MapOracleDataType(string dataType)
    {
        return dataType.ToUpper() switch
        {
            "VARCHAR2" => OracleDbType.Varchar2,
            "NUMBER" => OracleDbType.Decimal,
            "DATE" => OracleDbType.Date,
            "TIMESTAMP" => OracleDbType.TimeStamp,
            "CLOB" => OracleDbType.Clob,
            "BLOB" => OracleDbType.Blob,
            // ADD REF CURSOR => OracleDbType.RefCursor,
            "REF CURSOR" => OracleDbType.RefCursor,
            _ => OracleDbType.Varchar2
        };
    }
    
    private ParameterDirection MapParameterDirection(string direction)
    {
        return direction.ToUpper() switch
        {
            "IN" => ParameterDirection.Input,
            "OUT" => ParameterDirection.Output,
            "IN/OUT" => ParameterDirection.InputOutput,
            _ => ParameterDirection.Input
        };
    }
     [HttpPost]
    public async Task<IActionResult> Edit([FromBody] EditScheduledJobViewModel editDto)
    {
        
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }
        
        // Validate emails
        if (!string.IsNullOrWhiteSpace(editDto.SendToEmails) && !ValidateEmailList(editDto.SendToEmails))
        {
            return BadRequest("Invalid email format in Send To Emails");
        }
        
        if (!string.IsNullOrWhiteSpace(editDto.CCMails) && !ValidateEmailList(editDto.CCMails))
        {
            return BadRequest("Invalid email format in CC Emails");
        }
        //ensure values as valid with value types

        
        try
        {
            var job = await _context.ScheduledJobs.FindAsync(editDto.Id);
            if (job == null)
            {
                return NotFound("Scheduled job not found");
            }

            job.Description = editDto.Description;
            job.SchemaId = editDto.SchemaId;
            job.ProcedureName = editDto.ProcedureName;
            job.CronExpression = editDto.CronExpression;
            job.NextRunAt = CrontabSchedule.Parse(editDto.CronExpression).GetNextOccurrence(DateTime.Now);
           
            job.IsActive = editDto.IsActive;
            job.MessageSubject = editDto.MessageSubject;
            job.MessageBody = editDto.MessageBody;
            job.SendToEmails = editDto.SendToEmails;
            job.CCMails = editDto.CCMails;

            // Update parameters
            job.Parameters.Clear(); // Clear existing parameters
            //
            if (editDto.Parameters != null)
            {
                var updatedParameters = editDto.Parameters.Select(x => new ScheduledJobParameter
                {
                    Name = x.Name,
                    Value = x.Direction =="Input"? string.IsNullOrWhiteSpace(x.Value  )?throw new ArgumentNullException("Value is required for Input parameters") : x.Value.ToString() : null,
                    Direction = Enum.Parse<ParameterDirection>(x.Direction, true),
                    Type = Enum.Parse<OracleDbType>(x.Type, true)
                }).ToList();

                job.Parameters = updatedParameters;
            }
            // ensure that input parameters are valid with its Type
            foreach (var param in job.Parameters.Where(p => p.Direction == ParameterDirection.Input || p.Direction == ParameterDirection.InputOutput))
            {
                if (string.IsNullOrWhiteSpace(param.Value))
                {
                    throw new ArgumentException($"Value is required for Input parameter '{param.Name}'");
                }
                // Additional validation could be added here based on OracleDbType
                if (!IsValueValidForType(param.Value, param.Type))
                {
                    throw new ArgumentException($"Value '{param.Value}' is not valid for parameter '{param.Name}' of type {param.Type}");
                }
            }

            await _context.SaveChangesAsync();
           
            return Ok(new { message = "Scheduled job updated successfully" });
        }
        catch (Exception ex)
        {
             return BadRequest(ex.Message);
        }
    }

    private bool IsValueValidForType(string value, OracleDbType type)
    {
       
       return type switch
        {
            OracleDbType.Date =>
                DateTime.TryParse(value, out _),
            OracleDbType.Decimal =>
                decimal.TryParse(value, out _),
            OracleDbType.TimeStamp =>
                DateTime.TryParse(value, out _),
            _ => !string.IsNullOrWhiteSpace(value) // For other types, just check if value is not null/empty
        };
    }

    private bool ValidateEmailList(string emailList)
    {
        if (string.IsNullOrWhiteSpace(emailList))
            return true;
            
        var emailRegex = new Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$");
        var emails = emailList.Split(',').Select(e => e.Trim()).Where(e => !string.IsNullOrEmpty(e));
        
        return emails.All(email => emailRegex.IsMatch(email));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        var scheduledJob= await _context.ScheduledJobs.Include(j=>j.Schema).Include(j=>j.CreatedBy).FirstOrDefaultAsync(j=>j.Id==id);
        if(scheduledJob==null)
        {
            return NotFound();
        }
        var schemas = await _context.Schemas.Select(s=>new SelectListItem{
            Value=s.Id,
            Text=s.Name}).ToListAsync();
            ViewBag.Schemas=schemas;
            var jobResponse = new EditScheduledJobViewModel
            {
                Id = scheduledJob.Id,
                Description = scheduledJob.Description,
                SchemaId = scheduledJob.SchemaId,
                ProcedureName = scheduledJob.ProcedureName,
                CronExpression = scheduledJob.CronExpression,
                Parameters = scheduledJob.Parameters.Select(p => new EditScheduledJobParameter
                {
                    Name = p.Name,
                    Value = p.Value,
                    Direction = p.Direction.ToString(),
                    Type = p.Type.ToString()
                }).ToList(),
                IsActive = scheduledJob.IsActive,
                MessageSubject = scheduledJob.MessageSubject,
                MessageBody = scheduledJob.MessageBody,
                SendToEmails = scheduledJob.SendToEmails,
                CCMails = scheduledJob.CCMails
            };
            return View(jobResponse);
      
    }
        public async Task<IActionResult> Index()
    {
        var jobs = await _context.ScheduledJobs
            .Include(j=>j.Schema)
            .Include(j=>j.CreatedBy)
            .Select(j=>new ScheduledJobViewModel
            {
                Id=j.Id,
                Description=j.Description ?? "",
                Schema=j.Schema.Name,
                ProcedureName=j.ProcedureName,
                CronExpression=j.CronExpression,
                NextRunAt=j.NextRunAt,
                CreatedAt=j.CreatedAt,
                CreatedBy=j.CreatedBy.UserName ??"",
                IsActive=j.IsActive
            })
            .ToListAsync();
        return View(jobs);
    
    }
    // delete endpoint

    [HttpGet]
    public async Task<IActionResult> Delete(string id)
    {
      var job= await _context.ScheduledJobs
      .Include(j=>j.CreatedBy)
      .Include(j=>j.Schema).Where(j=>j.Id==id).Select(j=>new DeleteScheduledJobViewModel
      {
          Id=j.Id,
          Description=j.Description,
          Schema=j.Schema.Name,
          ProcedureName=j.ProcedureName,
          CronExpression=j.CronExpression,
          CreatedBy=j.CreatedBy.FullName
          
        
      }).FirstOrDefaultAsync();
      if(job==null)
      {
          return NotFound();
      }
      return View(job);
    }
    // confirm delete
    public async Task<IActionResult> ConfirmDelete(string id)
    {
        // delete the job
     var count=await   _context.ScheduledJobs.Where(j=>j.Id==id).ExecuteDeleteAsync();
     if(count==0)
      {
          return NotFound();
      }
     return RedirectToAction(nameof(Index));
    }

}
