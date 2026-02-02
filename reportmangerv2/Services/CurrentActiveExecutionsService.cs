using System;
using System.Collections.Concurrent;

namespace ReportManager.Services;

public class CurrentActiveExecutionsService
{
    private ConcurrentDictionary<string, ExecutionInfo> _activeExecutions;

    public CurrentActiveExecutionsService()
    {
        _activeExecutions = new ConcurrentDictionary<string, ExecutionInfo>();
    }

    public void AddExecution(string ExecutionId,string reportId, string UserId, DateTime executionTime, CancellationTokenSource cancellationTokenSource)
    {
        _activeExecutions.TryAdd(ExecutionId, new ExecutionInfo {  StartTime = executionTime, UserId = UserId, CancellationTokenSource = cancellationTokenSource, ReportId=reportId });
    }

    public bool IsExecutionActive(string ExecutionId)
    {
        return _activeExecutions.ContainsKey(ExecutionId);
    }

    public void RemoveExecution(string ExecutionId)
    {
        _activeExecutions.TryRemove(ExecutionId, out _);
    }
    public bool CancelExecution(string ExecutionId)
    {
        bool success = false;
        if (_activeExecutions.TryGetValue(ExecutionId, out ExecutionInfo? executionInfo) && executionInfo != null)
        {
            executionInfo.CancellationTokenSource.Cancel();
            success = true;
            
        }
        RemoveExecution(ExecutionId);
        return success;
    }

}
public class ExecutionInfo
{
    public required CancellationTokenSource CancellationTokenSource { get; set; } //= new CancellationTokenSource();
    public DateTime StartTime { get; set; }=DateTime.Now;
    public required string UserId { get; set; } 
    public string ReportId { get; set; } = "";
    
}