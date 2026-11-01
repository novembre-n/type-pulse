using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

namespace TypePulse.Tests;

public class ChatHubTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ChatHubTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Presence_BothClientsReceiveBothNames()
    {
        var connA = CreateConnection(_factory);
        var connB = CreateConnection(_factory);

        var tcsA = new TaskCompletionSource<string[]>();
        var tcsB = new TaskCompletionSource<string[]>();

        connA.On<string[]>("PresenceUpdated", users =>
        {
            if (users.Contains("Alice") && users.Contains("Bob"))
                tcsA.TrySetResult(users);
        });
        connB.On<string[]>("PresenceUpdated", users =>
        {
            if (users.Contains("Alice") && users.Contains("Bob"))
                tcsB.TrySetResult(users);
        });

        try
        {
            await connA.StartAsync();
            await connB.StartAsync();

            await connA.InvokeAsync("Join", "Alice");
            await connB.InvokeAsync("Join", "Bob");

            var resultA = await tcsA.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var resultB = await tcsB.Task.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.Contains("Alice", resultA);
            Assert.Contains("Bob", resultA);
            Assert.Contains("Alice", resultB);
            Assert.Contains("Bob", resultB);
        }
        finally
        {
            await connA.DisposeAsync();
            await connB.DisposeAsync();
        }
    }

    private static HubConnection CreateConnection(WebApplicationFactory<Program> factory)
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "/chat"), options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
            })
            .Build();
    }
}