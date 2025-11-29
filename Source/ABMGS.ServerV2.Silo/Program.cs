

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using StackExchange.Redis;
using SyncnetPlatform.Extensions;
using Microsoft.Extensions.DependencyInjection;
using SyncnetPlatform.Databases;

string? EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration
    .AddCommandLine(args)
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", false, true)
    .AddJsonFile($"appsettings.{EnvironmentName}.json", true, true)
    .AddEnvironmentVariables();

builder.AddSyncnetPlatformSilo(optionsBulider =>
{
    optionsBulider.UseBuiltinDbContext = false;
    optionsBulider.RegisterDbContext<SyncnetDbContextExtend>(builder);
});
builder.Services.AddScoped<SyncnetDbContext>(sp => sp.GetRequiredService<SyncnetDbContextExtend>());
var host = builder.Build();

await host.RunAsync();


