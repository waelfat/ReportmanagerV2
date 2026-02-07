using System;
using System.Data;
using System.Threading.Channels;

using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using reportmangerv2.Data;
using reportmangerv2.Domain;
using reportmangerv2.Enums;
using reportmangerv2.Services;

namespace ReportManager.Services;

public class ExecutionService : BackgroundService
{
    private Channel<ExecutionRequest> _channel;
    private readonly ILogger<ExecutionService> _logger;
    private CurrentActiveExecutionsService _currentActiveExecutionsService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IConfiguration _configuration;
    public ExecutionService(Channel<ExecutionRequest> channel, ILogger<ExecutionService> logger,
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
        await foreach (var message in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
              //  _logger.LogInformation($"Processing report execution for report {message.ReportTitle} with Execution ID {message.ExecutionId}");
                if (message.Type=="SQLSTATEMENT")await ProcessExecutions(message, stoppingToken);
                if (message.Type=="PROCEDURE") await ProcessStoredProcedureExecutions(message, stoppingToken); 
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Report execution was cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing report execution.");
            }
        }


    }
    private async Task ProcessExecutions(ExecutionRequest message, CancellationToken stoppingToken)
    {
     
        string executionId = string.Empty;
        OracleParameter[] executionParameters=[];
        var executionCancellationTokenSource = new CancellationTokenSource();
        var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, executionCancellationTokenSource.Token);

        try
        {

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                // Here you would add the logic to execute the report based on the message details
                // For example, fetch the report details from the database


                // Process the report execution
               //load the executionparameters
                var execution= await context.Executions.FirstOrDefaultAsync(e=>e.Id == message.ExecutionId) ?? throw new Exception("Execution not found");
               execution.ExecutionStatus = ExecutionStatus.Running;
               //execution.ExecutionDate = DateTime.UtcNow; // Set actual start time
               //load exection in 
               executionParameters = execution.ExecutionParameters.Select(p=> new OracleParameter(p.Name, (object?)p.Value ?? DBNull.Value)).ToArray();

            
                await context.SaveChangesAsync(linkedTokenSource.Token);
                if (execution == null) throw new Exception("Execution not found");
                executionId = execution.Id;
                var added=_currentActiveExecutionsService.AddExecution(message.ExecutionId, message.Id, message.UserId, execution.ExecutionDate, linkedTokenSource);
                if (!added)
                {
                    _logger.LogWarning("Execution already exists in CurrentActiveExecutionsService");
                }
               // report = await context.Reports.FindAsync(message.ReportId);
                _logger.LogInformation($"Started execution of report {message.ReportTitle} for user {message.UserId} with Execution ID {execution.Id}");

            }
           // await Task.Delay(TimeSpan.FromMinutes(2), linkedTokenSource.Token); // Simulate some initial delay
            OracleConnection connection = new OracleConnection(message.ConnectionString);
            OracleDataReader reader = ExecuteReportSQLStatement(message.SQLStatement, connection, executionParameters, linkedTokenSource.Token);


            var filePathFinal = await SaveOracleDataReaderToExcel(reader, message, linkedTokenSource.Token);
            #region previouscode
     
            #endregion

            await reader.CloseAsync();
            await connection.CloseAsync();
            // Update execution status
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var notificationService = scope.ServiceProvider.GetRequiredService<IExecutionNotificationService>();
                var execution = await context.Executions.FindAsync(executionId);
                if (execution != null)
                {
                    execution.ExecutionStatus = ExecutionStatus.Completed;
                    execution.ResultFilePath = filePathFinal;
                    execution.Duration = DateTime.Now - execution.ExecutionDate;

                    await context.SaveChangesAsync();
                    _currentActiveExecutionsService.RemoveExecution(executionId);
                    
                    // Send SignalR notification
                    await notificationService.NotifyExecutionCompleted(
                        executionId, 
                        "Succeeded", 
                        (int)execution.Duration.TotalSeconds, 
                        !string.IsNullOrEmpty(filePathFinal),
                        message.UserId
                    );
                    
                    _logger.LogInformation($"Completed execution of report {message.ReportTitle} for user {message.UserId} with Execution ID {execution.Id}");
                }
            }
            Console.WriteLine($"Executing report {message.ReportTitle} with parameters:");
           
            Console.WriteLine($"Report {message.ReportTitle} execution completed.");

        }
        
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging report execution start.");
               _logger.LogInformation($"Report {message.ReportTitle} execution was cancelled.");
            // Update execution status to Failed or Cancelled
            try
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var notificationService = scope.ServiceProvider.GetRequiredService<IExecutionNotificationService>();
                    var execution = await context.Executions.FirstOrDefaultAsync(e=>e.Id == executionId);
                    if (execution != null)
                    {
                        execution.ExecutionStatus = ex is OperationCanceledException ? ExecutionStatus.Cancelled : ExecutionStatus.Failed;
                        execution.Duration = DateTime.UtcNow - execution.ExecutionDate;
                        execution.ErrorMessage = ex is OperationCanceledException ? "Execution was cancelled by user." : "Execution failed.\n" + ex.Message;
                        execution.ResultFilePath = null;

                        await context.SaveChangesAsync();
                        _currentActiveExecutionsService.RemoveExecution(executionId);
                        
                        // Send SignalR notification
                        await notificationService.NotifyExecutionCompleted(
                            executionId, 
                            ex is OperationCanceledException ? "Cancelled" : "Failed", 
                            (int)execution.Duration.TotalSeconds, 
                            false,
                            message.UserId
                        );
                        
                        _logger.LogInformation($"Cancelled execution of report {message.Id} for user {message.UserId} with Execution ID {execution.Id}");
                    }
                }
            }
            catch (Exception saveErrorEx)
            {
                _logger.LogError(saveErrorEx, "Error logging report execution cancellation.");
            }
        }
        //delay for 5 minutes to simulate long running report

    }
    private async Task<string> SaveOracleDataReaderToExcel(OracleDataReader reader, ExecutionRequest message, CancellationToken cancellationToken = default)
    {
        var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Report");
        //  create uniue file name with date time and id
        string fileName = $"{message.ReportTitle}-{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid().ToString("N")[..5]}.xlsx";
        // string fileName = $"{message.ReportTitle}_{DateTime.Now.ToString("yyyyMMdd_HHmmss")}.xlsx";
        string filePath = Path.Combine(_webHostEnvironment.ContentRootPath, "Reports", fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        // Write column headers
        for (int i = 0; i < reader.FieldCount; i++)
        {
            worksheet.Cell(1, i + 1).Value = reader.GetName(i);
        }
        var currentRow = 2;
        await reader.ReadAsync();
        while (await reader.ReadAsync())
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                worksheet.Cell(currentRow, i + 1).Value = reader.GetValue(i).ToString();
            }
            currentRow++;
        }
        if (cancellationToken.IsCancellationRequested)
        {
            throw new OperationCanceledException(cancellationToken);
        }
        workbook.SaveAs(filePath);
        return filePath;
    }
    private OracleDataReader ExecuteReportSQLStatement(string sqlStatement, OracleConnection connection, OracleParameter[] parameters, CancellationToken executionCancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText = sqlStatement;
        // Add parameters to the command
        command.Parameters.AddRange(parameters);
 
        connection.Open();

        // Log current DB user and current schema and try to resolve unqualified table names (helps when different schema owns the table)
        try
        {
            using var infoCmd = connection.CreateCommand();
            infoCmd.CommandText = "SELECT USER FROM DUAL";
            var dbUser = infoCmd.ExecuteScalar()?.ToString();
            infoCmd.CommandText = "SELECT SYS_CONTEXT('USERENV','CURRENT_SCHEMA') FROM DUAL";
            var currentSchema = infoCmd.ExecuteScalar()?.ToString();
            _logger.LogInformation("Oracle connection opened. DB User = {DbUser}, Current Schema = {CurrentSchema}", dbUser, currentSchema);

            // If the SQL references a known table (e.g. AREAS_LIST) check its owner and set session schema so unqualified names resolve
            const string lookFor = "AREAS_LIST";
            if (sqlStatement.IndexOf(lookFor, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                infoCmd.Parameters.Clear();
                infoCmd.CommandText = "SELECT OWNER FROM ALL_OBJECTS WHERE UPPER(OBJECT_NAME) = :name AND ROWNUM = 1";
                infoCmd.Parameters.Add(new OracleParameter("name", lookFor.ToUpper()));
                var owner = infoCmd.ExecuteScalar()?.ToString();
                if (!string.IsNullOrEmpty(owner))
                {
                    _logger.LogInformation("Found object {ObjectName} owned by {Owner}", lookFor, owner);
                    if (!string.Equals(owner, currentSchema, StringComparison.OrdinalIgnoreCase))
                    {
                        using var alterCmd = connection.CreateCommand();
                        alterCmd.CommandText = ($"ALTER SESSION SET CURRENT_SCHEMA = {owner}");
                        alterCmd.ExecuteNonQuery();
                        _logger.LogInformation("Session CURRENT_SCHEMA set to {Owner} to resolve unqualified object names", owner);
                    }
                }
                else
                {
                    _logger.LogWarning("Object {ObjectName} not found in ALL_OBJECTS for this connection", lookFor);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while checking object ownership / setting CURRENT_SCHEMA");
        }

        OracleDataReader reader = command.ExecuteReader();
        return reader;
    }
    private async Task ProcessStoredProcedureExecutions(ExecutionRequest message, CancellationToken stoppingToken)
    {
        // Implement stored procedure execution logic here
        string executionId = string.Empty;
        OracleParameter[] executionParameters = [];
        
        var executionCancellationTokenSource = new CancellationTokenSource();
        var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, executionCancellationTokenSource.Token);
        try
        {

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                   var execution= await context.Executions.FirstOrDefaultAsync(e=>e.Id == message.ExecutionId) ?? throw new Exception("Execution not found");
               execution.ExecutionStatus = ExecutionStatus.Running;
               executionParameters = execution.ExecutionParameters.Select(p=> new OracleParameter(p.Name, p.Direction)
               {
                   Value = (object?)p.Value ?? DBNull.Value,

                   
               }).ToArray();
           
               
                await context.SaveChangesAsync(linkedTokenSource.Token);
                executionId = execution.Id;
            }
            await Task.Delay(TimeSpan.FromMinutes(2), executionCancellationTokenSource.Token); // Simulate some initial delay
          
            OracleConnection connection = new OracleConnection(message.ConnectionString);
            OracleCommand command = new OracleCommand(message.SQLStatement, connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddRange(executionParameters);
            await connection.OpenAsync(linkedTokenSource.Token);
            _currentActiveExecutionsService.AddExecution(executionId,message.Id,message.UserId,DateTime.Now,linkedTokenSource);
            _logger.LogInformation($"Executing stored procedure {message.ReportTitle} with parameters:");
            await command.ExecuteNonQueryAsync(linkedTokenSource.Token);
            // Retrieve output parameter values
            var outputParams = executionParameters.Where(p => p.Direction == ParameterDirection.Output || p.Direction == ParameterDirection.InputOutput).ToArray();
            foreach (var param in outputParams)
            {
                if (linkedTokenSource.Token.IsCancellationRequested)
                {
                    throw new OperationCanceledException(linkedTokenSource.Token);
                }
                
                if (command.Parameters[param.ParameterName].Value == DBNull.Value)
                {
                    _logger.LogInformation($"Output Parameter {param.ParameterName}: NULL");
                    continue;
                }
                if (param.OracleDbType == OracleDbType.RefCursor)
                {
                    OracleDataReader reader = ((OracleRefCursor)command.Parameters[param.ParameterName].Value).GetDataReader();
                    // Process the ref cursor as needed
                    _logger.LogInformation($"Output Parameter {param.ParameterName} is a RefCursor with {reader.FieldCount} fields.");
                    await SaveOracleDataReaderToExcel(reader, message, linkedTokenSource.Token);
                    await reader.CloseAsync();
                    continue;
                }
                var value = command.Parameters[param.ParameterName].Value;
                _logger.LogInformation($"Output Parameter {param.ParameterName}: {value}");
            }
            await connection.CloseAsync();
            _currentActiveExecutionsService.RemoveExecution(executionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing stored procedure execution.");
            _currentActiveExecutionsService.RemoveExecution(executionId);
        }
    }
}

public class ExecutionRequest
{

    public required string Id { get; set; }=Guid.NewGuid().ToString();
    public required string ReportTitle { get; set; }
    public required string  ExecutionId { get; set; }

    public required string SQLStatement { get; set; }
    public required string ConnectionString { get; set; }
    public required string UserId { get; set; }
    public required string Type { get; set; }="SQLSTATEMENT";



}

