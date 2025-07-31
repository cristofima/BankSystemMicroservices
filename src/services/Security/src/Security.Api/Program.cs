using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Scalar.AspNetCore;
using Security.Api;
using Security.Api.Middleware;
using Security.Application;
using Security.Infrastructure;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddWebApiServices(builder.Configuration);

var app = builder.Build();

// Development-specific middleware
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(options =>
    {
        options.WithTitle("Security API");
    });
}
else
{
    // Production-specific middleware
    app.UseHsts(); // HTTP Strict Transport Security
}

// Core middleware pipeline
app.UseHttpsRedirection();

// CORS (if needed for browser clients)
app.UseCors("DefaultPolicy");

// Rate limiting
app.UseRateLimiter();

// Token revocation middleware (must be before authentication)
app.UseMiddleware<TokenRevocationMiddleware>();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Health checks
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                exception = x.Value.Exception?.Message,
                duration = x.Value.Duration.ToString()
            }),
            duration = report.TotalDuration.ToString()
        };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
});

// Map controllers
app.Map("/", () => Results.Redirect("/scalar"));
app.MapControllers();

await app.RunAsync();

[ExcludeFromCodeCoverage]
public static partial class Program
{ }