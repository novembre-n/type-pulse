using TypePulse;
using TypePulse.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();
builder.Services.AddSingleton<PresenceStore>();

var app = builder.Build();

app.UseDefaultFiles();  // maps / to wwwroot/index.html.
app.UseStaticFiles();   // serves the generated JavaScript, CSS, and other assets.


app.MapHub<ChatHub>("/chat");

app.Run();
