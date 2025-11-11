using ABMGS.ServerV2.Grains;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();

// SyncnetPlatform Actors
builder.Services.AddTransient<IGameSessionActor, GameSessionActor>();
builder.Services.AddTransient<ICustomPacketHandler, CustomPacketHandler>();
builder.Services.AddTransient<FlatBufferPacketRouter>();


builder.UseOrleans(builder => {
    builder.UseLocalhostClustering();
    builder.AddMemoryGrainStorageAsDefault((OptionsBuilder<MemoryGrainStorageOptions> options) =>
    {
    });
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

