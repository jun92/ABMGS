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

if (builder.Configuration.GetValue<bool>("UseCloud") && !isGitHubActions)
{
    var postgresConnectionString = builder.AddConnectionString("SyncnetPlatform");
    var redisConnectionString = builder.AddConnectionString("redis");
    var silo = builder.AddProject<Projects.ABMGS_ServerV2_Silo>("silo")
        .WaitFor(redisConnectionString)
        .WaitFor(postgresConnectionString)
        .WithReference(redisConnectionString)
        .WithReference(postgresConnectionString)
        ;
   builder.AddProject<Projects.ABMGS_ServerV2>("orleans-frontend")
    .WaitFor(redisConnectionString)
    .WaitFor(postgresConnectionString)
    .WithReference(silo)
    .WithReference(redisConnectionString)
    .WithReference(postgresConnectionString)
    ;


}
else
{

    var redis = builder.AddRedis("redis");
    var postgresPassword = builder.AddParameter("postgres-password", secret: true);
    var rdbms = builder
        .AddPostgres("npgsql", password: postgresPassword)
        .WithDataVolume("syncnet-pg-data")
        .AddDatabase("SyncnetPlatform");

    var silo = builder.AddProject<Projects.ABMGS_ServerV2_Silo>("silo")
        .WaitFor(redis)
        .WaitFor(rdbms)
        .WithReference(redis)
        .WithReference(rdbms)
        ;
    

    builder.AddProject<Projects.ABMGS_ServerV2>("orleans-frontend")
        .WaitFor(redis)
        .WaitFor(rdbms)
        .WithReference(silo)
        .WithReference(redis)
        .WithReference(rdbms)
        ;
}




builder.Build().Run();
