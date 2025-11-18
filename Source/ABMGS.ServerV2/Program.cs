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
using SyncnetPlatform.Controllers;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.AddServiceDefaults();

// SyncnetPlatform Actors
builder.UseSyncnetPlatform();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
app.UseRouting();
app.MapDefaultEndpoints();
app.MapControllers();
app.UseHttpsRedirection();
app.UseWebSockets();
app.UseHealthChecks("/health");

await app.RunAsync();

