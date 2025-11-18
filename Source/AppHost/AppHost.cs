using Aspire.Hosting.Orleans;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");

var silo = builder.AddProject<Projects.ABMGS_ServerV2_Silo>("silo")
    .WithReference(redis);

builder.AddProject<Projects.ABMGS_ServerV2>("orleans-frontend")
    .WithReference(silo)
    .WithReference(redis);


builder.Build().Run();
