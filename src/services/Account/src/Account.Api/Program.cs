using BankSystem.Account.Api;
using BankSystem.Account.Api.Middlewares;
using BankSystem.Account.Application;
using BankSystem.Account.Infrastructure;
using BankSystem.Shared.ServiceDefaults.Extensions;
using System.Diagnostics.CodeAnalysis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddWebApiServices(builder.Configuration);

var app = builder.Build();

// Exception handling middleware (Account-specific)
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Use service defaults middleware pipeline
app.UseServiceDefaults("Account API");

// Map controllers
app.MapControllers();

await app.RunAsync();

[ExcludeFromCodeCoverage]
public static partial class Program
{ }