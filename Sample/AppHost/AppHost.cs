using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var jetflowdb = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgWeb()
    .AddDatabase("jetflow", "jetflow");

var nats = builder.AddNats("nats")
    .WithJetStream()
    .WithDataVolume();

builder.AddProject<InventoryService>("inventory")
    .WithReference(nats)
    .WaitFor(nats);

builder.AddProject<PaymentService>("payment")
    .WithReference(nats)
    .WaitFor(nats);

var processor = builder.AddProject<OrderProcessor>("orderProcessor")
    .WithReference(nats)
    .WaitFor(nats);

builder.AddProject<ShippingService>("shipping")
    .WithReference(nats)
    .WaitFor(nats);

builder.AddProject<WebUI>("webui")
    .WithReference(jetflowdb)
    .WithReference(nats)
    .WaitFor(jetflowdb)
    .WaitFor(nats)
    .WithExternalHttpEndpoints();

builder.AddProject<OrderSubmitter>("ordersubmitter")
    .WithReference(nats)
    .WaitFor(nats)
    .WaitFor(processor);

builder.Build().Run();
