using System.Diagnostics.CodeAnalysis;
using BankSystem.Account.Api;
using BankSystem.Account.Api.Middlewares;
using BankSystem.Account.Application;
using BankSystem.Account.Infrastructure;
using BankSystem.ServiceDefaults;
using BankSystem.Shared.WebApiDefaults.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Add services to the container
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddWebApiServices(builder.Configuration);

var app = builder.Build();
app.MapDefaultEndpoints();

// Exception handling middleware (Account-specific)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Use service defaults middleware pipeline
app.UseWebApiDefaults("Account API");

// Map controllers
app.MapControllers();

await app.RunAsync();

[ExcludeFromCodeCoverage]
public static partial class Program { }
