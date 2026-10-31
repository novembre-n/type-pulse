using Microsoft.AspNetCore.SignalR;
using TypePulse;

namespace TypePulse.Hubs;

public class ChatHub : Hub
{
    private readonly PresenceStore _presenceStore;

    public ChatHub(PresenceStore presenceStore)
    {
        _presenceStore = presenceStore;
    }

    public async Task Join(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new HubException("A display name is required.");
        }

        var users = _presenceStore.AddOrUpdateUser(
            Context.ConnectionId,
            displayName.Trim());

        await Clients.All.SendAsync("PresenceUpdated", users);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var users = _presenceStore.RemoveUser(Context.ConnectionId);

        await Clients.All.SendAsync("PresenceUpdated", users);
        await base.OnDisconnectedAsync(exception);
    }    
}