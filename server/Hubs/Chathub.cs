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

    public async Task SendMessage(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new HubException("A message is required.");
        }

        var name = _presenceStore.GetName(Context.ConnectionId);

        if (name is null)
        {
            throw new HubException("Join before you send a message.");
        }

        await Clients.All.SendAsync("MessageReceived", name, text.Trim());
    }

    public async Task UserTyping()
    {
        var name = _presenceStore.GetName(Context.ConnectionId);

        if (name is null)
        {
            return;
        }

        await Clients.Others.SendAsync("UserTyping", name);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var users = _presenceStore.RemoveUser(Context.ConnectionId);

        await Clients.All.SendAsync("PresenceUpdated", users);
        await base.OnDisconnectedAsync(exception);
    }    
}