using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SyncnetPlatform.Databases;

namespace SyncnetPlatform.Extensions.Options;

public class SyncnetSiloOptionsBuilder
{
    public bool UseBuiltinDbContext { get; set; }

    public void RegisterDbContext<DbContextType>(WebApplicationBuilder builder) where DbContextType : SyncnetDbContext
    {
        UseBuiltinDbContext = false;
        builder.Services.AddDbContextPool<DbContextType>(opt =>
        {
            opt.UseNpgsql(builder.Configuration.GetConnectionString("postgres"), optionBuilder =>
            {
                optionBuilder.MigrationsAssembly(typeof(DbContextType).Assembly.FullName);
            });
        });
        builder.Services.AddScoped<SyncnetDbContext>(sp => sp.GetRequiredService<DbContextType>());
    }
}


