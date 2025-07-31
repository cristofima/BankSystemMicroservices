using BankSystem.Shared.ServiceDefaults.Extensions;
using Security.Api;
using Security.Api.Middleware;
using Security.Application;
using Security.Infrastructure;
using System.Diagnostics.CodeAnalysis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddWebApiServices(builder.Configuration);

var app = builder.Build();

// Use service defaults middleware pipeline
app.UseServiceDefaults("Security API");

// Token revocation middleware (must be before authentication but after service defaults)
app.UseMiddleware<TokenRevocationMiddleware>();

// Map controllers
app.MapControllers();

await app.RunAsync();

[ExcludeFromCodeCoverage]
public static partial class Program
{ }