using Microsoft.AspNetCore.SignalR;

namespace CodeExplainer.WebApi.Hubs;

public class ChatHub : Hub
{
    public async Task SendMessage(string sessionId, string user, string message)
    {
        await Clients.Group(sessionId).SendAsync("ReceiveMessage", user, message);
    }

    public async Task JoinSession(string sessionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, sessionId);
        await Clients.Group(sessionId).SendAsync("SystemMessage", $"{Context.ConnectionId} joined session {sessionId}");
    }

    public async Task LeaveSession(string sessionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, sessionId);
    }
}