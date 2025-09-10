using ABMGS.Server.Front.Interfaces.Players;
using ABMGS.Server.Front.Services;
using ABMGS.Server.Front.Services.Player;
using Orleans.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi


builder.Services.AddHostedService<SessionService>();
builder.Services.AddSingleton<DefaultPlayerFactory>();
builder.Services.AddTransient<PlayerLoopService>();
builder.Services.AddTransient<IPlayerFactory, DefaultPlayerFactory>();
builder.Services.AddControllers();
builder.UseOrleans(builder =>
{
    builder.Configure<ClusterOptions>(options =>
    {
        options.ClusterId = "ABMGSCluster";
        options.ServiceId = "ABMGSApp";
    });

    //builder.UseLocalhostClustering();
    builder.UseDevelopmentClustering( options =>     {
        options.PrimarySiloEndpoint = new System.Net.IPEndPoint(System.Net.IPAddress.Loopback, 11111);
    });
    builder.Use
});
var app = builder.Build();

app.UseWebSockets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
   // app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();
