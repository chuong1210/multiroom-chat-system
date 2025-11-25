using Blazored.LocalStorage;
using ChatRoomSystem.Client.Components;
using ChatRoomSystem.Client.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Blazored LocalStorage
builder.Services.AddBlazoredLocalStorage();

// HttpClient with base address
builder.Services.AddScoped(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["ApiSettings:BaseUrl"] ?? "http://localhost:5000";
    return new HttpClient { BaseAddress = new Uri(baseUrl) };
});

// Application Services
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ApiService>();
builder.Services.AddScoped<WebSocketService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

Console.WriteLine("=================================================");
Console.WriteLine("  Chat Room System - Blazor Client");
Console.WriteLine("=================================================");
Console.WriteLine($"  Client URL: http://localhost:5001");
Console.WriteLine($"  API Backend: {builder.Configuration["ApiSettings:BaseUrl"]}");
Console.WriteLine("=================================================");

app.Run();
