

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using StackExchange.Redis;
using SyncnetPlatform.Extensions;

string? EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddCommandLine(args)
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", false, true)
    .AddJsonFile($"appsettings.{EnvironmentName}.json", true, true)
    .AddEnvironmentVariables();

builder.AddSyncnetPlatformSilo();

var host = builder.Build();

await host.RunAsync();


