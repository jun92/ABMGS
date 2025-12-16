using SyncnetPlatform.Extensions;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
// builder.AddServiceDefaults();

// SyncnetPlatform Actors
builder.AddSyncnetPlatformFrontend();

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseFrontendSyncnetPlatform();
app.UseRouting();
app.MapDefaultEndpoints();
app.MapControllers();
app.UseHttpsRedirection();
app.UseHealthChecks("/health");

await app.RunAsync();

