using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SyncnetPlatform.Databases;

//public class SyncnetDbContextFactory : IDesignTimeDbContextFactory<SyncnetDbContext>
//{
//    protected HostApplicationBuilder _applicationBuilder;
//    public SyncnetDbContextFactory() : base()
//    {
//        string? environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

//        _applicationBuilder = Host.CreateApplicationBuilder();

//        _applicationBuilder.Configuration
//            .SetBasePath(Directory.GetCurrentDirectory())
//            .AddJsonFile("appsettings.json", false, true)
//            .AddJsonFile($"appsettings.{environmentName}.json", true, true)
//            .AddEnvironmentVariables();
//    }
//    public SyncnetDbContext CreateDbContext(string[] args)
//    {
//        var options = new DbContextOptionsBuilder<SyncnetDbContext>()
//            .UseNpgsql(_applicationBuilder.Configuration.GetConnectionString("SyncnetPlatform"), options => {
//                options.MigrationsAssembly("Package");
//            })
//            .Options;

//        return new SyncnetDbContext(options);
//    }
//}
