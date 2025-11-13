using ABMGS.ServerV2.SyncnetPlatform.Extensions;
using ABMGS.ServerV2.SyncnetPlatform.Interfaces.Network.Handlers;
using ABMGS.ServerV2.SyncnetPlatform.Interfaces.Network.Sessions;
using ABMGS.ServerV2.SyncnetPlatform.Network.Handlers;
using ABMGS.ServerV2.SyncnetPlatform.Network.Sessions;
using ABMGS.ServerV2.SyncnetPlatform.Network.Utils;
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

// SyncnetPlatform Actors
builder.UseSyncnetPlatform();
//builder.AddCustomPacketHandler<CustomPacketHandler>();



builder.UseOrleans(builder => {
    builder.UseRedisClustering(options => {
        options.ConfigurationOptions = ConfigurationOptions.Parse("localhost:6379");
    });
    //builder.UseLocalhostClustering();
    //builder.AddMemoryGrainStorageAsDefault((OptionsBuilder<MemoryGrainStorageOptions> options) =>
    //{
    //});
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseWebSockets();

await app.RunAsync();

