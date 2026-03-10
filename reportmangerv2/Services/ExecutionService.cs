using System;
using System.Data;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Threading.Channels;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.CodeAnalysis.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using reportmangerv2.Data;
using reportmangerv2.Domain;
using reportmangerv2.Enums;
using reportmangerv2.Events;
using reportmangerv2.Services;

namespace ReportManagerv2.Services;

public class ExecutionService : BackgroundService
{
    private Channel<ExecutionRequest> _channel;
    private readonly ILogger<ExecutionService> _logger;
    private CurrentActiveExecutionsService _currentActiveExecutionsService;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IWebHostEnvironment _webHostEnvironment;
    private readonly IConfiguration _configuration;
    private readonly IEmailSender _emailSender;
    private readonly IEventPublisher _eventPublisherl;
    private readonly ReportManagerSettings _settings;
    public ExecutionService(Channel<ExecutionRequest> channel, ILogger<ExecutionService> logger,
    CurrentActiveExecutionsService currentActiveExecutionsService,
    IServiceScopeFactory serviceScopeFactory,
    IWebHostEnvironment webHostEnvironment,
    IEmailSender emailSender,
    IConfiguration configuration,
    IEventPublisher eventPublisher,
    IOptions<ReportManagerSettings> option
    )
    {
        _channel = channel;
        _logger = logger;
        _currentActiveExecutionsService = currentActiveExecutionsService;
        _serviceScopeFactory = serviceScopeFactory;
        _webHostEnvironment = webHostEnvironment;
        _configuration = configuration;
        _emailSender = emailSender;
        _eventPublisherl = eventPublisher;
        _settings = option.Value;

    }
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {

        try
        {
            await foreach (var message in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    //  _logger.LogInformation($"Processing report execution for report {message.ReportTitle} with Execution ID {message.ExecutionId}");
                    _logger.LogInformation($"Execution Type: {message.Type}");
                    switch (message.Type)
                    {
                        case ExecutionRequesType.SqlStatement:
                            _ =  ProcessExecutions(message, stoppingToken);
                            break;
                        case ExecutionRequesType.StoredProcedure:
                            _ =  ProcessStoredProcedureExecutions(message, stoppingToken);
                            break;
                        default:
                            _logger.LogWarning($"Unknown execution type: {message.Type}");
                            throw new ArgumentOutOfRangeException(nameof(message.Type), message.Type, null);
                    }
                    // if (message.Type=="SQLSTATEMENT")await ProcessExecutions(message, stoppingToken);
                    // if (message.Type=="PROCEDURE") await ProcessStoredProcedureExecutions(message, stoppingToken); 
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
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // swallow the exception
            _logger.LogInformation("ExecutionService is stopping due to cancellation.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ExecutionService.");
        }
    }
    private async Task ProcessExecutions(ExecutionRequest message, CancellationToken stoppingToken)
    {

        string executionId = string.Empty;
        OracleParameter[] executionParameters = [];
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
                var execution = await context.Executions.FirstOrDefaultAsync(e => e.Id == message.ExecutionId) ?? throw new Exception("Execution not found");
                execution.ExecutionStatus = ExecutionStatus.Running;
                //execution.ExecutionDate = DateTime.UtcNow; // Set actual start time
                //load exection in 
                executionParameters = execution.ExecutionParameters.Select(p => new OracleParameter
               // (p.Name, p.Direction,Value=(object?)p.Value ?? DBNull.Value,OracleDbType=p.Type}
                  { ParameterName = p.Name, Direction = p.Direction, Value = (object?)p.Value ?? DBNull.Value, OracleDbType = p.Type })
                
                .ToArray();
                // if param is date convert value to date
                for (int i = 0; i < executionParameters.Length; i++)
                {
                    if (executionParameters[i].OracleDbType == OracleDbType.Date && executionParameters[i].Value != null && executionParameters[i].Value != DBNull.Value)
                    {
                        if (DateTime.TryParse(executionParameters[i].Value.ToString(), out DateTime dateValue))
                        {
                            executionParameters[i].Value = dateValue;
                        }
                    }
                }
                await _eventPublisherl.PublishAsync(new ExecutionStartedEvent
                {
                    ExecutionId = message.ExecutionId,
                    UserId = message.UserId,
                }, linkedTokenSource.Token);


                await context.SaveChangesAsync(linkedTokenSource.Token);

                if (execution == null) throw new Exception("Execution not found");
                executionId = execution.Id;
                var added = _currentActiveExecutionsService.AddExecution(message.ExecutionId, message.Id, message.UserId, execution.ExecutionDate, linkedTokenSource);
                if (!added)
                {
                    _logger.LogWarning("Execution already exists in CurrentActiveExecutionsService");
                }
                // report = await context.Reports.FindAsync(message.ReportId);
                _logger.LogInformation($"Started execution of report {message.ReportTitle} for user {message.UserId} with Execution ID {execution.Id}");

            }
            //  await Task.Delay(TimeSpan.FromMinutes(2), linkedTokenSource.Token); // Simulate some initial delay
            OracleConnection connection = new(message.ConnectionString);
            //start execting with params 
            _logger.LogInformation($"Executing SQL statement for report {message.ReportTitle} with Execution ID {executionId}. params: {string.Join(",",executionParameters.Select(p=>p.ParameterName + ":" + p.Value))}");
             var baseFile   = $"report_{message.ReportTitle}_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
            OracleDataReader reader = await ExecuteReportSQLStatement(message.SQLStatement, connection, executionParameters, linkedTokenSource.Token);
          (string filePath,long count) result;
           using (var scope = _serviceScopeFactory.CreateScope())
            {
                var xlsxwriter=scope.ServiceProvider.GetRequiredService<IXlsxWriterService>();
            //var filePathFinal = await SaveOracleDataReaderToExcel(reader, message, linkedTokenSource.Token);
                result=await xlsxwriter.WriteQueryAsync(reader,_settings.OutputDirectory,baseFile,linkedTokenSource.Token);
            }
            

            #region previouscode

            #endregion

            await reader.CloseAsync();
            await connection.CloseAsync();
            //linkedTokenSource.Token.ThrowIfCancellationRequested();
            // Update execution status
            #region  appdbcontextmethod
            /*
            
            
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var notificationService = scope.ServiceProvider.GetRequiredService<IExecutionNotificationService>();
                var execution = await context.Executions.Include(c=>c.ScheduledJob).FirstOrDefaultAsync(c=>c.Id== executionId);
                if (execution != null)
                {
                    execution.ExecutionStatus = ExecutionStatus.Completed;
                    execution.ResultFilePath = filePathFinal;
                    execution.Duration = DateTime.Now - execution.ExecutionDate;
                    //if scheduledjobid is not null then its a scheduled query modify job and deactivated it
                    if (execution.ScheduledJob != null)
                    {
                        execution.ScheduledJob.JobStatus = ExecutionStatus.Pending;
                   
                        execution.ScheduledJob.IsActive = false;

                        
                    }
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
            
            
            
            */


            #endregion

            ExecutionCompletedEvent completed = new ExecutionCompletedEvent()
            {
                ExecutionId = executionId,
                ExecutionStatus = ExecutionStatus.Completed,
                IsSuccessful = true,
                ResultFilePaths = result.filePath,
                ErrorMessage = string.Empty,
                UserId = message.UserId
            };
            await _eventPublisherl.PublishAsync(completed, linkedTokenSource.Token);
            Console.WriteLine($"Executing report {message.ReportTitle} with parameters:");

            Console.WriteLine($"Report {message.ReportTitle} execution completed.");

        }
        catch (OperationCanceledException)
        {
            var reason = stoppingToken.IsCancellationRequested ? "Host is shutting down" : $"User{message.UserId} cancelled execution";
            _logger.LogInformation($"Report {message.ReportTitle} execution was cancelled. Reason: {reason}");
            ExecutionCompletedEvent cancelled = new()
            {
                ExecutionId = executionId,
                ExecutionStatus = ExecutionStatus.Cancelled,
                IsSuccessful = false,
                ResultFilePaths = string.Empty,
                ErrorMessage = reason,
                UserId = message.UserId
            };
            await _eventPublisherl.PublishAsync(cancelled);

        }

        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging report execution start.");
            _logger.LogInformation($"Report {message.ReportTitle} execution was cancelled.");
            // Update execution status to Failed or Cancelled

            //The filename, directory name, or volume label syntax is incorrect. : 'E:\projects\dotnetprojects\vscodeprojects\reportmanagerv2\reportmangerv2\Reports\select * from AREAS_LIST where AREA_ID > :TotalAmount-20260214_144206_ed0e3.xlsx'.
            #region usingadodirectly
            /*
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var notificationService = scope.ServiceProvider.GetRequiredService<IExecutionNotificationService>();
                var execution = await context.Executions.FirstOrDefaultAsync(e => e.Id == executionId);
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
            */
            #endregion
            ExecutionCompletedEvent @event = new()
            {
                ExecutionId = executionId,
                ExecutionStatus = ExecutionStatus.Failed,
                IsSuccessful = false,
                ResultFilePaths = string.Empty,
                ErrorMessage = ex.Message,
                UserId = message.UserId
            };
            await _eventPublisherl.PublishAsync(@event);

        }
        finally
        {
            executionCancellationTokenSource.Dispose();
        }
        //delay for 5 minutes to simulate long running report

    }
    
    private async Task<string> SaveOracleDataReaderToExcelxml(OracleDataReader reader, ExecutionRequest message, CancellationToken cancellationToken = default)
    {
        //using xml to save large files
        var sheetsCount = 1;

        var currentRow = 2;
        var extractedRows = 0;
        string fileName = $"{message.ReportTitle}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        string filePath = Path.Combine(_webHostEnvironment.ContentRootPath, "Reports", fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
     using (SpreadsheetDocument document = SpreadsheetDocument.Create(filePath, SpreadsheetDocumentType.Workbook))
{
    WorkbookPart workbookPart = document.AddWorkbookPart();
    workbookPart.Workbook = new Workbook();
    // write in batches every 1000 rows
   // var batch=new List<object[]>();

    // Create Sheets collection once
    Sheets sheets = workbookPart.Workbook.AppendChild(new Sheets());

     sheetsCount = 1;
    WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
    OpenXmlWriter writer = OpenXmlWriter.Create(worksheetPart);

    try
    {
        writer.WriteStartElement(new Worksheet());
        writer.WriteStartElement(new SheetData());

        // Header row
        writer.WriteStartElement(new Row());
        for (int i = 0; i < reader.FieldCount; i++)
        {
            writer.WriteElement(new Cell { DataType = CellValues.String, CellValue = new CellValue(reader.GetName(i)) });
        }
        writer.WriteEndElement(); // Row

        // Register first sheet
        sheets.Append(new Sheet
        {
            Id = workbookPart.GetIdOfPart(worksheetPart),
            SheetId = (uint)sheetsCount,
            Name = "Report"
        });

        // Data rows
        while (await reader.ReadAsync(cancellationToken))
        {
            writer.WriteStartElement(new Row());
            for (int i = 0; i < reader.FieldCount; i++)
            {

               // writer.WriteElement(new Cell { DataType = CellValues.String, CellValue = new CellValue(reader.GetValue(i).ToString()) });
               writer.WriteElement(CreateCell(reader.GetValue(i)));
            }
            writer.WriteEndElement(); // Row

            currentRow++;
            extractedRows++;

            if (currentRow % 10000 == 0)
            {
                await _eventPublisherl.PublishAsync(new ReportProgressEvent
                {
                    ExecutionId = message.ExecutionId,
                    ProgressRowsNumber = extractedRows,
                    UserId = message.UserId
                });
            }

            if (currentRow > 1048000)
            {
                // Close current sheet
                writer.WriteEndElement(); // SheetData
                writer.WriteEndElement(); // Worksheet
                writer.Dispose();

                // New sheet
                sheetsCount++;
                worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                writer = OpenXmlWriter.Create(worksheetPart);

                writer.WriteStartElement(new Worksheet());
                writer.WriteStartElement(new SheetData());

                // Header row for new sheet
                writer.WriteStartElement(new Row());
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    writer.WriteElement(new Cell { DataType = CellValues.String, CellValue = new CellValue(reader.GetName(i)) });
                }
                writer.WriteEndElement(); // Row

                // Register new sheet
                sheets.Append(new Sheet
                {
                    Id = workbookPart.GetIdOfPart(worksheetPart),
                    SheetId = (uint)sheetsCount,
                    Name = $"Report_{sheetsCount}"
                });

                currentRow = 2;
            }
        }

        writer.WriteEndElement(); // SheetData
        writer.WriteEndElement(); // Worksheet
    }
    finally
    {
        writer?.Dispose();
    }
}

        return filePath;
    }
private CellValues DetermineCellValueType(object value)
    {
        if (value == null || value == DBNull.Value)
        {
            return CellValues.String;
        }

        return value switch
        {
            string => CellValues.String,
            int => CellValues.Number,
            long => CellValues.Number,
            short => CellValues.Number,
            byte => CellValues.Number,
            sbyte => CellValues.Number,
            ushort => CellValues.Number,
            uint => CellValues.Number,
            ulong => CellValues.Number,
            float => CellValues.Number,
            double => CellValues.Number,
            decimal => CellValues.Number,
            bool => CellValues.Boolean,
            DateTime => CellValues.Date,
            _ => CellValues.String
        };
      //  if (value is int || value is long || value is short || value is byte || value is sbyte || value is ushort || value is uint || value is ulong)
    }
    private Cell CreateCell(object value)
    {
        var cell = new Cell();
        if (value == null || value == DBNull.Value)
        {
            cell.DataType = CellValues.String;
            cell.CellValue = new CellValue("");
        }
        else
        {
            var cellType = DetermineCellValueType(value);
            cell.DataType = cellType;
            cell.CellValue = new CellValue(value.ToString() ?? "");
        }
        return cell;
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

        // if (!reader.HasRows)
        //     return string.Empty;
        while (await reader.ReadAsync(cancellationToken))
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                worksheet.Cell(currentRow, i + 1).Value = reader.GetValue(i).ToString();
            }
            currentRow++;
            // await for 2 seconds

            if (currentRow % 10 == 0)
            {
                // raise event Progress
                await _eventPublisherl.PublishAsync(new ReportProgressEvent() { ExecutionId = message.ExecutionId, ProgressRowsNumber = currentRow, UserId = message.UserId });

            }
            //await Task.Delay(2000, cancellationToken);
        }
        //cancellationToken.ThrowIfCancellationRequested();
        workbook.SaveAs(filePath);
        return filePath;
    }
    private async Task<OracleDataReader> ExecuteReportSQLStatement(string sqlStatement, OracleConnection connection, OracleParameter[] parameters, CancellationToken executionCancellationToken)
    {
        var command = connection.CreateCommand();
        command.CommandText = sqlStatement;
        // Add parameters to the command
        command.Parameters.AddRange(parameters);

        await connection.OpenAsync(executionCancellationToken);

        // Log current DB user and current schema and try to resolve unqualified table names (helps when different schema owns the table)
        // try
        // {
        //     using var infoCmd = connection.CreateCommand();
        //     infoCmd.CommandText = "SELECT USER FROM DUAL";
        //     var dbUser = infoCmd.ExecuteScalar()?.ToString();
        //     infoCmd.CommandText = "SELECT SYS_CONTEXT('USERENV','CURRENT_SCHEMA') FROM DUAL";
        //     var currentSchema = infoCmd.ExecuteScalar()?.ToString();
        //     _logger.LogInformation("Oracle connection opened. DB User = {DbUser}, Current Schema = {CurrentSchema}", dbUser, currentSchema);

        //     // If the SQL references a known table (e.g. AREAS_LIST) check its owner and set session schema so unqualified names resolve
        //     const string lookFor = "AREAS_LIST";
        //     if (sqlStatement.IndexOf(lookFor, StringComparison.OrdinalIgnoreCase) >= 0)
        //     {
        //         infoCmd.Parameters.Clear();
        //         infoCmd.CommandText = "SELECT OWNER FROM ALL_OBJECTS WHERE UPPER(OBJECT_NAME) = :name AND ROWNUM = 1";
        //         infoCmd.Parameters.Add(new OracleParameter("name", lookFor.ToUpper()));
        //         var owner = infoCmd.ExecuteScalar()?.ToString();
        //         if (!string.IsNullOrEmpty(owner))
        //         {
        //             _logger.LogInformation("Found object {ObjectName} owned by {Owner}", lookFor, owner);
        //             if (!string.Equals(owner, currentSchema, StringComparison.OrdinalIgnoreCase))
        //             {
        //                 using var alterCmd = connection.CreateCommand();
        //                 alterCmd.CommandText = ($"ALTER SESSION SET CURRENT_SCHEMA = {owner}");
        //                 alterCmd.ExecuteNonQuery();
        //                 _logger.LogInformation("Session CURRENT_SCHEMA set to {Owner} to resolve unqualified object names", owner);
        //             }
        //         }
        //         else
        //         {
        //             _logger.LogWarning("Object {ObjectName} not found in ALL_OBJECTS for this connection", lookFor);
        //         }
        //     }
        // }
        // catch (Exception ex)
        // {
        //     _logger.LogWarning(ex, "Error while checking object ownership / setting CURRENT_SCHEMA");
        // }

        OracleDataReader reader = await command.ExecuteReaderAsync(executionCancellationToken);
        return reader;
    }
    private async Task ProcessStoredProcedureExecutions(ExecutionRequest message, CancellationToken stoppingToken)
    {
        // Implement stored procedure execution logic here
        string executionId = string.Empty;
        OracleParameter[] executionParameters = [];
        var outfilesList = new List<string>();
        var executionCancellationTokenSource = new CancellationTokenSource();
        var linkedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, executionCancellationTokenSource.Token);
        try
        {

            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var execution = await context.Executions.AsNoTracking().FirstOrDefaultAsync(e => e.Id == message.ExecutionId, linkedTokenSource.Token) ?? throw new Exception("Execution not found");
                      //create oracle parametrs with length of 4000 for varchar2 and nvarchar

                executionParameters = execution.ExecutionParameters.Select(p => new OracleParameter
                {
                    Direction = p.Direction,
                    Value = (object?)p.Value ?? DBNull.Value,
                    ParameterName = p.Name,
                    OracleDbType = p.Type, // p.Type must be OracleDbType Direction = p.Direction, // ParameterDirection Value = p.Value ?? DBNull.Value
                  // Size = (p.Type == OracleDbType.Varchar2 || p.Type == OracleDbType.NVarchar2) ? 4000 : default

                }).ToArray();
                // for each nvarchar2 or varchar2 assign size to 4000
                for (int i = 0; i < executionParameters.Length; i++)
                {
                    if (executionParameters[i].OracleDbType == OracleDbType.Varchar2 || executionParameters[i].OracleDbType == OracleDbType.NVarchar2)
                    {
                        executionParameters[i].Size = 4000;
                    }
                }

                //await context.SaveChangesAsync(linkedTokenSource.Token);
                await _eventPublisherl.PublishAsync(new ExecutionStartedEvent() { ExecutionId = execution.Id, UserId = message.UserId });
                executionId = execution.Id;
            }
            //q   7      await Task.Delay(TimeSpan.FromSeconds(100), linkedTokenSource.Token); // Simulate some initial delay

            OracleConnection connection = new OracleConnection(message.ConnectionString);
            OracleCommand command = new OracleCommand(message.SQLStatement, connection);
            command.CommandType = CommandType.StoredProcedure;
            command.Parameters.AddRange(executionParameters);
            await connection.OpenAsync(linkedTokenSource.Token);
            _currentActiveExecutionsService.AddExecution(executionId, message.Id, message.UserId, DateTime.Now, linkedTokenSource);
            _logger.LogInformation($"Executing stored procedure {message.ReportTitle} with parameters:");
            await command.ExecuteNonQueryAsync(linkedTokenSource.Token);
            // Retrieve output parameter values
            var outputParams = executionParameters.Where(p => p.Direction == ParameterDirection.Output || p.Direction == ParameterDirection.InputOutput).ToArray();

            foreach (var param in outputParams)
            {
                linkedTokenSource.Token.ThrowIfCancellationRequested();

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
                    string filePath = await SaveOracleDataReaderToExcelxml(reader, message, linkedTokenSource.Token);

                    await reader.CloseAsync();
                    if (!string.IsNullOrWhiteSpace(filePath)) outfilesList.Add(filePath);
                    _logger.LogInformation($"RefCursor data saved to {filePath}");
                    continue;
                }
                var value = command.Parameters[param.ParameterName].Value;
                _logger.LogInformation($"Output Parameter {param.ParameterName}: {value}");
            }
            await connection.CloseAsync();
            // mark job as pending


        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing stored procedure execution.");
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                _logger.LogInformation($"Cancelled execution of report {message.Id} for user {message.UserId} with Execution ID {executionId}");
                await _eventPublisherl.PublishAsync(new ExecutionFailedEvent { ExecutionId = executionId, ErrorMessage = ex.Message, UserId = message.UserId });
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var execution = context.Executions.FirstOrDefault(e => e.Id == message.ExecutionId);
                if (execution != null)
                {
                    execution.ExecutionStatus = ExecutionStatus.Failed;
                    execution.ErrorMessage = ex.Message;
                    execution.Duration = DateTime.UtcNow - execution.ExecutionDate;

                }
                _logger.LogError(ex, "Error processing stored procedure execution for ExecutionId: {ExecutionId}", message.ExecutionId);
                // reset scheduled job 
                var failedJob = await context.ScheduledJobs.FirstOrDefaultAsync(s => s.Id == message.ExecutionId, linkedTokenSource.Token);
                failedJob?.JobStatus = ExecutionStatus.Pending;

                _ = await context.SaveChangesAsync(linkedTokenSource.Token);
                _currentActiveExecutionsService.CancelExecution(executionId);
                return;
            }
        }
        string fileslist = string.Empty;
        // concatinate files names with comma
        if (!(outfilesList.Count == 0))
            fileslist = string.Join(",", outfilesList);
        using (var scope = _serviceScopeFactory.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var execution = await context.Executions.FirstOrDefaultAsync(e => e.Id == message.ExecutionId, linkedTokenSource.Token);
            if (execution != null)
            {
                execution.ExecutionStatus = ExecutionStatus.Completed;
                execution.Duration = DateTime.UtcNow - execution.ExecutionDate;
                execution.ResultFilePath = fileslist;//filePath; // Store the file path
                execution.ErrorMessage = $"No Data retruned at {DateTime.Now.ToLongTimeString()}";
            }

            // mark job as pending
            var job = await context.ScheduledJobs.FirstOrDefaultAsync(s => s.Id == message.Id, linkedTokenSource.Token);
            job?.JobStatus = ExecutionStatus.Pending;
            // 
            _ = await context.SaveChangesAsync(linkedTokenSource.Token);
        }
        try
        {

            // send files by mail
            if (message.To != null)
            {

                await _emailSender.SendEmailAsync(message.To, message.Subject ?? string.Empty, string.IsNullOrWhiteSpace(fileslist) ? $"No Data retruned at {DateTime.Now.ToLongTimeString()}" : message.Body ?? string.Empty, fileslist, message.CC ?? string.Empty);
            }
        }
        catch (System.Exception e)
        {
            _logger.LogError(e, "Error sending email");


        }




    }
}

public class ExecutionRequest
{

    public required string Id { get; set; } = Guid.NewGuid().ToString();
    public required string ReportTitle { get; set; }
    public required string ExecutionId { get; set; }

    public required string SQLStatement { get; set; }
    public required string ConnectionString { get; set; }
    public required string UserId { get; set; }
    public required ExecutionRequesType Type { get; set; } = ExecutionRequesType.SqlStatement;
    public string? Emails { get; set; }
    public string? CC { get; set; }
    public string? Body { get; set; }
    public string? To { get; set; }
    public string? Subject { get; set; }



}

