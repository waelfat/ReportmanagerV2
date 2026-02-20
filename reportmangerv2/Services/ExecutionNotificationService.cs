using Microsoft.AspNetCore.SignalR;
using reportmangerv2.Hubs;

namespace reportmangerv2.Services
{
    public interface IExecutionNotificationService
    {
        Task NotifyExecutionCompleted(string executionId, string status, int duration, bool hasResult, string userId);
        Task NotifyExecutionStarted(string executionId, string reportName,string userId);
        Task NotifyReportProgress(string executionId, int? progress, string userId);
        Task NotifyReportFailed (string executionId, string errorMessage, string userId);
    }

    public class ExecutionNotificationService : IExecutionNotificationService
    {
        private readonly IHubContext<ExecutionHub> _hubContext;

        public ExecutionNotificationService(IHubContext<ExecutionHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyExecutionCompleted(string executionId, string status, int duration, bool hasResult, string userId)
        {
            await _hubContext.Clients.User(userId).SendAsync("ExecutionCompleted", executionId, status, duration, hasResult);
            // notify admins
            await _hubContext.Clients.Group("Admins").SendAsync("ExecutionCompleted", executionId, status, duration, hasResult);
        }

        public async Task NotifyExecutionStarted(string executionId,string reportName, string userId)
        {
           
           await _hubContext.Clients.User(userId).SendAsync("ExecutionStarted", executionId,reportName);
           await _hubContext.Clients.Group("Admins").SendAsync("ExecutionStarted", executionId, reportName);
           
        }

        public async Task NotifyReportFailed(string executionId, string errorMessage, string userId)
        {
           await _hubContext.Clients.User(userId).SendAsync("ExecutionFailed", executionId, errorMessage);
            await _hubContext.Clients.Group("Admins").SendAsync("ExecutionFailed", executionId, errorMessage);
        }


        public async Task NotifyReportProgress(string executionId, int? progress, string userId)
        {
            await _hubContext.Clients.User(userId).SendAsync("ExecutionProgress", executionId, progress);
            await _hubContext.Clients.Group("Admins").SendAsync("ExecutionProgress", executionId, progress);
        }
        // override onconnectasync to add admin to admins Group
      

    }
}