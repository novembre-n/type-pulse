# TypePulse

A small real-time chat. One room, no accounts, no history.

Users join with a display name. They see who is online, send messages, and see a typing indicator. Everything lives in memory. A restart clears the room.

## Stack

- **Server:** .NET 10, ASP.NET Core, SignalR
- **Client:** TypeScript, Vite, Tailwind CSS
- **Transport:** WebSocket, with SignalR fallback
- **Tests:** xUnit
- **Container:** Docker, Docker Compose

## Packages

Server: SignalR ships with ASP.NET Core. The server project has no package references.

Client (`client/package.json`):

- `@microsoft/signalr` — browser client for the hub
- `vite` — build tool and dev server
- `typescript` — type checker
- `tailwindcss` and `@tailwindcss/vite` — styles

Tests (`test/TypePulse.Tests/TypePulse.Tests.csproj`):

- `xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`
- `Microsoft.AspNetCore.SignalR.Client` — test clients
- `Microsoft.AspNetCore.Mvc.Testing` — in-memory test server
- `coverlet.collector` — coverage

## Run with Docker Compose

```bash
docker compose up -d --build
```

Open <http://localhost:8080>.

Stop it:

```bash
docker compose down
```

The image builds the frontend with Node, publishes the server with the .NET SDK, and runs the result on the ASP.NET runtime. The final image contains no build tools.

## How it works

The browser connects to `/chat`. `ChatHub` receives `Join`, `SendMessage`, and `UserTyping`. `PresenceStore` keeps the connection ID to display name map in memory. The server broadcasts `PresenceUpdated`, `MessageReceived`, and `UserTyping` to the clients.

## Tests

```bash
dotnet test test/TypePulse.Tests/TypePulse.Tests.csproj
```

Four tests: presence, message delivery, typing relay, and disconnect cleanup.
