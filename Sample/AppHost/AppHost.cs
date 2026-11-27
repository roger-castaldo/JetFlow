using AppHost.NUI;

var builder = DistributedApplication.CreateBuilder(args);

var jetflowdb = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgWeb()
    .AddDatabase("jetflow", "jetflow");

var nats = builder.AddNats("nats")
    .WithJetStream()
    .WithDataVolume()
    .WithNui();

builder.AddDotnetProject("inventory","../InventoryService/InventoryService.csproj")
    .WithReference(nats)
    .WaitFor(nats);

builder.AddDotnetProject("payment","../PaymentService/PaymentService.csproj")
    .WithReference(nats)
    .WaitFor(nats);

var processor = builder.AddDotnetProject("orderProcessor","../OrderProcessor/OrderProcessor.csproj")
    .WithReference(nats)
    .WaitFor(nats);

builder.AddDotnetProject("shipping","../ShippingService/ShippingService.csproj")
    .WithReference(nats)
    .WaitFor(nats);

builder.AddDotnetProject("webui","../WebUI/WebUI.csproj")
    .WithReference(jetflowdb)
    .WithReference(nats)
    .WaitFor(jetflowdb)
    .WaitFor(nats)
    .WithExternalHttpEndpoints();

builder.AddDotnetProject("ordersubmitter","../OrderSubmitter/OrderSubmitter.csproj")
    .WithReference(nats)
    .WaitFor(nats)
    .WaitFor(processor);

builder.Build().Run();
