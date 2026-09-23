using InventoryService.Activities;
using JetFlow;
using Microsoft.Extensions.Configuration;
using Shared.dto;

var sourceCancel = new CancellationTokenSource();

Console.CancelKeyPress += delegate {
    sourceCancel.Cancel();
};

var config = (IConfiguration)(new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables()
    .AddUserSecrets(typeof(Program).Assembly)
    .Build());

var primaryConnection = await Connection.CreateInstanceAsync(new(new NATS.Client.Core.NatsOpts()
{
    Url = config.GetConnectionString("nats")!
})
{ 
    Namespace = Shared.Constants.PrimaryNamespace
});
await primaryConnection.RegisterWorkflowActivityWithReturnAsync<ReserveInventory, bool, IEnumerable<OrderItem>>();
await primaryConnection.RegisterWorkflowActivityAsync<PullInventory, IEnumerable<OrderItem>>();
await primaryConnection.RegisterWorkflowActivityAsync<UnreserveInventory, IEnumerable<OrderItem>>();

var secondaryConnection = await Connection.CreateInstanceAsync(new(new NATS.Client.Core.NatsOpts()
{
    Url = config.GetConnectionString("nats")!
})
{
    Namespace = Shared.Constants.SecondayNamespace
});
await secondaryConnection.RegisterWorkflowActivityWithReturnAsync<ReserveInventory, bool, IEnumerable<OrderItem>>();
await secondaryConnection.RegisterWorkflowActivityAsync<PullInventory, IEnumerable<OrderItem>>();
await secondaryConnection.RegisterWorkflowActivityAsync<UnreserveInventory, IEnumerable<OrderItem>>();


sourceCancel.Token.WaitHandle.WaitOne();

await ((IAsyncDisposable)primaryConnection).DisposeAsync();
await ((IAsyncDisposable)secondaryConnection).DisposeAsync();