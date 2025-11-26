using Aspire.Hosting;
using Aspire.Hosting.Orleans;

var builder = DistributedApplication.CreateBuilder(args);

var redis = builder.AddRedis("redis");
var rdbms = builder.AddPostgres("npgsql");

var silo = builder.AddProject<Projects.ABMGS_ServerV2_Silo>("silo")
    .WaitFor(redis)
    .WaitFor(rdbms)
    .WithReference(redis)
    .WithReference(rdbms)
    .WithReplicas(2);
    ;

    

builder.AddProject<Projects.ABMGS_ServerV2>("orleans-frontend")
    .WaitFor(redis)
    .WaitFor(rdbms)
    .WithReference(silo)
    .WithReference(redis)
    .WithReference(rdbms);


builder.Build().Run();
