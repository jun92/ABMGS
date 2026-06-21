using System;
using System.Collections.Generic;
using System.Text;

namespace SyncnetPlatform.ApplicationBuilder;


public static class SyncnetApplicationBuilder
{
    public static SyncnetFrontApplicationBuilder CreateFrontBuilder(string[] args)
    {
        return new SyncnetFrontApplicationBuilder(args); 
    }
    public static SyncnetActorApplicationBuilder CreateActorBuilder(string[] args)
    {
        return new SyncnetActorApplicationBuilder(args);
    }
}
