using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SyncnetPlatform.ApplicationBuilder;

public abstract class SyncnetBaseApplicationBuilder<TBuilder, TApp> where TBuilder: SyncnetBaseApplicationBuilder<TBuilder,TApp>
{
    protected readonly WebApplicationBuilder Builder;
    public ConfigurationManager Configuration => Builder.Configuration;
    public IServiceCollection Services => Builder.Services;
    public ILoggingBuilder Logging => Builder.Logging;
    public ConfigureWebHostBuilder WebHost => Builder.WebHost;
    public ConfigureHostBuilder Host => Builder.Host;
    public IWebHostEnvironment Environment => Builder.Environment;

    protected SyncnetBaseApplicationBuilder(string[] args)
    {
        Builder = WebApplication.CreateBuilder(args);
    }
    public abstract TApp Build();
}
