using Microsoft.AspNetCore.SignalR;
using reportmangerv2.Hubs;

namespace reportmangerv2.Services
{
    public interface IExecutionNotificationService
    {
        Task NotifyExecutionCompleted(string executionId, string status, int duration, bool hasResult, string userId);
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
        }
    }
}