using Microsoft.AspNetCore.SignalR;

namespace reportmangerv2.Hubs
{
    public class ExecutionHub : Hub
    {
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }
        // override onconnectasync to add admins to admins group
        public override async Task OnConnectedAsync()
        {
            var user = Context.User;
            if (user != null && user.IsInRole("Admin"))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Admins");
            }
            await base.OnConnectedAsync();
        }
       
    }
}