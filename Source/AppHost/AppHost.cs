using Aspire.Hosting.Orleans;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");
var rdbms = builder.AddPostgres("pgsql").AddDatabase("syncnet-platform");

var silo = builder.AddProject<Projects.ABMGS_ServerV2_Silo>("silo")
    .WithReference(redis)
    .WithReference(rdbms)
    .WithReplicas(2);

builder.AddProject<Projects.ABMGS_ServerV2>("orleans-frontend")
    .WithReference(silo)
    .WithReference(redis)
    .WithReference(rdbms);


builder.Build().Run();
