using SyncnetPlatform.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddSyncnetPlatformFrontend();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// builder.Services.AddOpenApi();
builder.Services.AddControllers();
// builder.Services.AddHealthChecks();
// builder.AddServiceDefaults();

// SyncnetPlatform Actors

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseFrontendSyncnetPlatform();

app.MapControllers();
//app.UseEndpoints(endpoints =>
//{
//    endpoints.MapControllers();
//});

await app.RunAsync();

