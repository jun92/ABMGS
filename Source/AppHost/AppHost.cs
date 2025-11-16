using Aspire.Hosting.Orleans;

var builder = DistributedApplication.CreateBuilder(args);

//var redis = builder.AddRedis("redis");
builder.AddProject<Projects.ABMGS_ServerV2>("orleans-frontend");
    //.WithReference(redis);

builder.Build().Run();
