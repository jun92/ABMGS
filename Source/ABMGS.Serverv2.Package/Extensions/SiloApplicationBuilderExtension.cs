using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Orleans.Configuration;
using Orleans.Dashboard;
using StackExchange.Redis;
using SyncnetPlatform.Databases;
using SyncnetPlatform.Extensions.Options;
using SyncnetPlatform.Interfaces.Network.Handlers;
using SyncnetPlatform.Interfaces.Network.Utils;
using SyncnetPlatform.Network.Utils;
using SyncnetPlatform.Repositories;

namespace SyncnetPlatform.Extensions;

public static class SiloApplicationBuilderExtension
{
    //public static void AddSyncnetPlatformSilo(
    //    this WebApplicationBuilder builder,
    //    Action<SyncnetSiloOptionsBuilder>? optionBuilder)
    //{
    //    //SyncnetPlatformSiloCommon(builder);

    //    SyncnetSiloOptionsBuilder options = new();
    //    options.UseBuiltinDbContext = true;
    //    if(optionBuilder != null)
    //    {
    //        optionBuilder(options);
    //    }

    //    if (options.UseBuiltinDbContext)
    //    {
    //        SyncnetPlatformSiloDbContext(builder);
    //    }
    //}
}


