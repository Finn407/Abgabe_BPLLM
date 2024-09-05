using BlazorApp1.Components;
using multimodalInputs.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using BlazorApp1.Data;
using Toolbelt.Blazor.Extensions.DependencyInjection;  //Speech Recognition

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddSpeechRecognition();//Speech Recognition
builder.Services.AddSingleton<StringService>();// Register the StringService as a singleton
builder.Services.AddTransient<LLMService>(); // Register LLMService here
builder.Services.AddScoped<IOCrService, TesseractOcrService>();
var app = builder.Build();

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