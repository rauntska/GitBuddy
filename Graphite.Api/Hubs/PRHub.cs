using Microsoft.AspNetCore.SignalR;

namespace Graphite.Api.Hubs;

public class PRHub : Hub
{
    private readonly ILogger<PRHub> _logger;

    public PRHub(ILogger<PRHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier ?? "anonymous";
        var userName = Context.User?.Identity?.Name ?? "anonymous";
        _logger.LogInformation("Client connected to PRHub: {ConnectionId} (User: {UserName})", Context.ConnectionId, userName);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userName = Context.User?.Identity?.Name ?? "anonymous";
        if (exception != null)
        {
            _logger.LogWarning("Client disconnected from PRHub with error: {ConnectionId} (User: {UserName}) - {Error}", 
                Context.ConnectionId, userName, exception.Message);
        }
        else
        {
            _logger.LogInformation("Client disconnected from PRHub: {ConnectionId} (User: {UserName})", Context.ConnectionId, userName);
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinPRRoom(int prId)
    {
        var groupName = GetPRGroupName(prId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("Client {ConnectionId} joined PR room: {GroupName}", Context.ConnectionId, groupName);
    }

    public async Task LeavePRRoom(int prId)
    {
        var groupName = GetPRGroupName(prId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        _logger.LogDebug("Client {ConnectionId} left PR room: {GroupName}", Context.ConnectionId, groupName);
    }

    public static string GetPRGroupName(int prId) => $"pr-{prId}";
}
