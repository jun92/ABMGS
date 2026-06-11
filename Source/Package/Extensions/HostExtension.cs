using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SyncnetPlatform.Databases;
using System;
using System.Collections.Generic;
using System.Text;

namespace SyncnetPlatform.Extensions;

public static class IHostExtension
{
    //public static IHost SyncnetDbMigrate<DbContextType>(this IHost host) where DbContextType : SyncnetDbContext
    //{
    //    using var scope = host.Services.CreateScope();
    //    var db = scope.ServiceProvider.GetRequiredService<DbContextType>();
    //    db.Database.Migrate();
    //    return host;
    //}
    public static IHost SyncnetDbMigrate(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SyncnetDbContext>();
        db.Database.Migrate();
        return host;
    }
}
