using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var jetflowdb = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgWeb()
    .AddDatabase("jetflow", "jetflow");

var nats = builder.AddNats("nats")
    .WithJetStream()
    .WithDataVolume();

builder.AddProject<WebUI>("webui")
    .WithReference(jetflowdb)
    .WithReference(nats)
    .WaitFor(jetflowdb)
    .WaitFor(nats)
    .WithExternalHttpEndpoints();

builder.Build().Run();
