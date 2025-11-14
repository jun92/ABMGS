using SyncnetPlatform.Extensions;
using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Interfaces.Network.Sessions;
using SyncnetPlatform.Network.Handlers;
using SyncnetPlatform.Network.Sessions;
using SyncnetPlatform.Network.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Orleans.Clustering.Redis;
using Orleans.Configuration;
using Orleans.Services;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.AddServiceDefaults();

// SyncnetPlatform Actors
builder.UseSyncnetPlatform();
//builder.AddCustomPacketHandler<CustomPacketHandler>();

// Orleans Configuration
builder.UseOrleans( builder => {
    builder.UseRedisClustering(options =>
    {
        options.ConfigurationOptions = ConfigurationOptions.Parse(
            builder.Configuration.GetConnectionString("redis") ?? throw new InvalidOperationException());
    });
    //builder.UseLocalhostClustering();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.MapDefaultEndpoints();
app.UseHttpsRedirection();
app.UseWebSockets();

await app.RunAsync();

