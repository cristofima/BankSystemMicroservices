using System.Diagnostics.CodeAnalysis;
using BankSystem.ServiceDefaults;
using BankSystem.Shared.WebApiDefaults.Extensions;
using Security.Api;
using Security.Api.Middleware;
using Security.Api.Services;
using Security.Application;
using Security.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();

// Add services to the container
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddWebApiServices(builder.Configuration);

// Add gRPC services with shared defaults
builder.Services.AddGrpcDefaults(builder.Configuration);

var app = builder.Build();
app.MapDefaultEndpoints();

// Token revocation middleware
app.UseMiddleware<TokenRevocationMiddleware>();

// Use service defaults middleware pipeline
app.UseWebApiDefaults("Security API");

// Use gRPC defaults middleware pipeline
app.UseGrpcDefaults();

// Map gRPC service with environment-aware authentication
app.MapGrpcServiceWithAuth<UserContactGrpcService>();

await app.RunAsync();

[ExcludeFromCodeCoverage]
public static partial class Program { }
