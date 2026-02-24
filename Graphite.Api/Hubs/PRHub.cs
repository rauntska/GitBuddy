using Microsoft.AspNetCore.SignalR;

namespace Graphite.Api.Hubs;

public class PRHub(ILogger<PRHub> logger) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userName = Context.User?.Identity?.Name ?? "anonymous";
        logger.LogInformation("Client connected to PRHub: {ConnectionId} (User: {UserName})", Context.ConnectionId, userName);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userName = Context.User?.Identity?.Name ?? "anonymous";
        if (exception != null)
        {
            logger.LogWarning("Client disconnected from PRHub with error: {ConnectionId} (User: {UserName}) - {Error}", 
                Context.ConnectionId, userName, exception.Message);
        }
        else
        {
            logger.LogInformation("Client disconnected from PRHub: {ConnectionId} (User: {UserName})", Context.ConnectionId, userName);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinPRRoom(int prId)
    {
        var groupName = GetPRGroupName(prId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        logger.LogDebug("Client {ConnectionId} joined PR room: {GroupName}", Context.ConnectionId, groupName);
    }

    public async Task LeavePRRoom(int prId)
    {
        var groupName = GetPRGroupName(prId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        logger.LogDebug("Client {ConnectionId} left PR room: {GroupName}", Context.ConnectionId, groupName);
    }

    public static string GetPRGroupName(int prId) => $"pr-{prId}";
}
