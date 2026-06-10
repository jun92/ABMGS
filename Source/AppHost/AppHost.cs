using Aspire.Hosting;
using Aspire.Hosting.Orleans;
using Microsoft.Extensions.Configuration;
using System.Diagnostics.CodeAnalysis;

var builder = DistributedApplication.CreateBuilder(args);

string EnvironmentName = Environment.GetEnvironmentVariable("ASPIRE_ENVIRONMENT") ?? "Development";

builder.Configuration.AddCommandLine(args).SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", false, true)
    .AddJsonFile($"appsettings.{EnvironmentName}.json", true, true)
    .AddEnvironmentVariables();

bool isGitHubActions = Environment.GetEnvironmentVariable("GITHUB_ACTIONS") == "true";

IResourceBuilder<IResourceWithConnectionString> postgres;
IResourceBuilder<IResourceWithConnectionString> redis;

if (builder.Configuration.GetValue<bool>("UseCloud") && !isGitHubActions)
{
    postgres = builder.AddConnectionString("postgres");
    redis = builder.AddConnectionString("redis");
}
else
{
    redis = builder.AddRedis("redis");
    var postgresPassword = builder.AddParameter("postgres-password", secret: true);
    postgres = builder
        .AddPostgres("npgsql", password: postgresPassword)
        .WithDataVolume("syncnet-pg-data")
        .AddDatabase("postgres", databaseName: "SyncnetPlatform");
}
var silo = builder.AddProject<Projects.Silo>("silo")
      .WaitFor(redis)
      .WaitFor(postgres)
      .WithReference(redis)
      .WithReference(postgres)
      ;

builder.AddProject<Projects.Front>("orleans-frontend")
    .WaitFor(redis)
    .WaitFor(postgres)
    .WithReference(silo)
    .WithReference(redis)
    .WithReference(postgres)
    ;

builder.Build().Run();
