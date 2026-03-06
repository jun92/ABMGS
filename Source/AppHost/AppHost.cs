using Aspire.Hosting;
using Aspire.Hosting.Orleans;

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    "Aspire.Hosting",
    "ASPIRECERTIFICATES001",
    Justification = "CI environment uses plaintext Redis without TLS"
)]

var builder = DistributedApplication.CreateBuilder(args);
#pragma warning disable ASPIRECERTIFICATES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var redis = builder.AddRedis("redis").WithoutHttpsCertificate();
#pragma warning restore ASPIRECERTIFICATES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var postgresPassword = builder.AddParameter("postgres-password", secret: true);
var rdbms = builder
    .AddPostgres("npgsql", password: postgresPassword)
    .WithDataVolume("syncnet-pg-data")
    .AddDatabase("SyncnetPlatform");

var silo = builder.AddProject<Projects.ABMGS_ServerV2_Silo>("silo")
    .WaitFor(redis)
    .WaitFor(rdbms)
    .WithReference(redis)
    .WithReference(rdbms)
    .WithReplicas(2)
    ;

    

builder.AddProject<Projects.ABMGS_ServerV2>("orleans-frontend")
    .WaitFor(redis)
    .WaitFor(rdbms)
    .WithReference(silo)
    .WithReference(redis)
    .WithReference(rdbms)
    ;


builder.Build().Run();
