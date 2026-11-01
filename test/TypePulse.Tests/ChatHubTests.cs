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

    [Fact]
    public async Task Message_DeliveredToBothClients()
    {
        var connA = CreateConnection(_factory);
        var connB = CreateConnection(_factory);

        var receivedA = new TaskCompletionSource<(string Name, string Text)>();
        var receivedB = new TaskCompletionSource<(string Name, string Text)>();

        connA.On<string, string>("MessageReceived", (name, text) =>
        {
            if (text == "hello")
                receivedA.TrySetResult((name, text));
        });
        connB.On<string, string>("MessageReceived", (name, text) =>
        {
            if (text == "hello")
                receivedB.TrySetResult((name, text));
        });

        var presenceA = new TaskCompletionSource<string[]>();
        var presenceB = new TaskCompletionSource<string[]>();

        connA.On<string[]>("PresenceUpdated", users =>
        {
            if (users.Contains("Alice") && users.Contains("Bob"))
                presenceA.TrySetResult(users);
        });
        connB.On<string[]>("PresenceUpdated", users =>
        {
            if (users.Contains("Alice") && users.Contains("Bob"))
                presenceB.TrySetResult(users);
        });

        try
        {
            await connA.StartAsync();
            await connB.StartAsync();

            await connA.InvokeAsync("Join", "Alice");
            await connB.InvokeAsync("Join", "Bob");

            await presenceA.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await presenceB.Task.WaitAsync(TimeSpan.FromSeconds(5));

            await connA.InvokeAsync("SendMessage", "hello");

            var resultA = await receivedA.Task.WaitAsync(TimeSpan.FromSeconds(5));
            var resultB = await receivedB.Task.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.Equal("Alice", resultA.Name);
            Assert.Equal("hello", resultA.Text);
            Assert.Equal("Alice", resultB.Name);
            Assert.Equal("hello", resultB.Text);
        }
        finally
        {
            await connA.DisposeAsync();
            await connB.DisposeAsync();
        }
    }

    [Fact]
    public async Task Typing_RelayedToOtherClientOnly()
    {
        var connA = CreateConnection(_factory);
        var connB = CreateConnection(_factory);

        var typingB = new TaskCompletionSource<string>();

        connB.On<string>("UserTyping", name => typingB.TrySetResult(name));

        // Alice should never receive her own typing event.
        var typingA = new TaskCompletionSource<string>();
        connA.On<string>("UserTyping", name => typingA.TrySetResult(name));

        var presenceA = new TaskCompletionSource<string[]>();
        var presenceB = new TaskCompletionSource<string[]>();

        connA.On<string[]>("PresenceUpdated", users =>
        {
            if (users.Contains("Alice") && users.Contains("Bob"))
                presenceA.TrySetResult(users);
        });
        connB.On<string[]>("PresenceUpdated", users =>
        {
            if (users.Contains("Alice") && users.Contains("Bob"))
                presenceB.TrySetResult(users);
        });

        try
        {
            await connA.StartAsync();
            await connB.StartAsync();

            await connA.InvokeAsync("Join", "Alice");
            await connB.InvokeAsync("Join", "Bob");

            await presenceA.Task.WaitAsync(TimeSpan.FromSeconds(5));
            await presenceB.Task.WaitAsync(TimeSpan.FromSeconds(5));

            await connA.InvokeAsync("UserTyping");

            var name = await typingB.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal("Alice", name);

            // Alice must not receive her own typing event.
            var completed = await Task.WhenAny(typingA.Task, Task.Delay(1000));
            Assert.NotEqual(typingA.Task, completed);
        }
        finally
        {
            await connA.DisposeAsync();
            await connB.DisposeAsync();
        }
    }

    [Fact]
    public async Task Disconnect_RemovesUserFromPresence()
    {
        var connA = CreateConnection(_factory);
        var connB = CreateConnection(_factory);

        var presenceA = new TaskCompletionSource<string[]>();
        var presenceAfterDisconnect = new TaskCompletionSource<string[]>();

        connA.On<string[]>("PresenceUpdated", users =>
        {
            if (users.Contains("Alice") && users.Contains("Bob"))
                presenceA.TrySetResult(users);
            else if (users.Contains("Alice") && !users.Contains("Bob"))
                presenceAfterDisconnect.TrySetResult(users);
        });

        try
        {
            await connA.StartAsync();
            await connB.StartAsync();

            await connA.InvokeAsync("Join", "Alice");
            await connB.InvokeAsync("Join", "Bob");

            await presenceA.Task.WaitAsync(TimeSpan.FromSeconds(5));

            await connB.DisposeAsync();

            var result = await presenceAfterDisconnect.Task.WaitAsync(TimeSpan.FromSeconds(5));

            Assert.Contains("Alice", result);
            Assert.DoesNotContain("Bob", result);
        }
        finally
        {
            await connA.DisposeAsync();
        }
    }

    private static HubConnection CreateConnection(WebApplicationFactory<Program> factory)
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri(factory.Server.BaseAddress, "/chat"), options =>
            {
                /*
                    * The test server is in-memory. It does not listen on a real port. 
                    * The SignalR client did not know that. 
                    * It tried to open a real TCP connection to localhost and failed.
                    * route the SignalR client through the test server's handler.    
                */
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
            })
            .Build();
    }
}