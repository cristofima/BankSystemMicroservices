using BankSystem.ServiceDefaults;
using BankSystem.Shared.WebApiDefaults.Extensions;
using Security.Api;
using Security.Api.Middleware;
using Security.Application;
using Security.Infrastructure;
using System.Diagnostics.CodeAnalysis;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Add services to the container
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddWebApiServices(builder.Configuration);

var app = builder.Build();
app.MapDefaultEndpoints();

// Token revocation middleware
app.UseMiddleware<TokenRevocationMiddleware>();

// Use service defaults middleware pipeline
app.UseWebApiDefaults("Security API");

// Map controllers
app.MapControllers();

await app.RunAsync();

[ExcludeFromCodeCoverage]
public static partial class Program
{ }