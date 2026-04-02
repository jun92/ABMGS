using SyncnetPlatform.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddSyncnetPlatformFrontend();
builder.Services.AddControllers();


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

