var builder = DistributedApplication.CreateBuilder(args);

// Projects
var security = builder
    .AddProject<Projects.Security_Api>("security");

var account = builder
    .AddProject<Projects.BankSystem_Account_Api>("account");

builder
    .AddProject<Projects.BankSystem_ApiGateway>("api-gateway")
    .WaitFor(security)
    .WaitFor(account);

await builder.Build().RunAsync();