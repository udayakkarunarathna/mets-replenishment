using METS.Blazor.Services;
using MudBlazor.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddMudServices();

// API client pointing at the METS.Api project
builder.Services.AddHttpClient<MetsApiClient>(client =>
    client.BaseAddress = new Uri("http://localhost:5000/"));

// Singleton session (scoped is fine for server-side Blazor per-circuit)
builder.Services.AddScoped<UserSession>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
    app.UseHsts();

app.UseStaticFiles();
app.UseRouting();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run("http://localhost:5001");
