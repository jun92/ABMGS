using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Orleans.Configuration;
using Orleans.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
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



app.MapGet("/", () =>
{
    return Results.Ok();
})
.WithName("Default");

app.Run();

