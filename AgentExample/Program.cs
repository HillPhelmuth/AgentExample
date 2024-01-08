using AgentExample;
using AgentExample.Components;
using AgentExample.Services;
using ChatComponents;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var services = builder.Services;
services.AddRazorComponents()
    .AddInteractiveServerComponents();
services.AddScoped<AgentRunnerService>();
services.AddChat();
services.AddRadzenComponents();
var config = builder.Configuration;
var app = builder.Build();
TestConfiguration.Initialize(config);
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
