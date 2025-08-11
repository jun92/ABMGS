using ABMGS.Server.Front.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi


builder.Services.AddHostedService<SessionService>();
builder.Services.AddTransient<PlayerLoopService>();
builder.Services.AddTransient<Player>();
builder.Services.AddControllers();

var app = builder.Build();

app.UseWebSockets();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
   // app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();
