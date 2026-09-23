using JetFlow;
using Microsoft.Extensions.Configuration;
using Shared.dto;
using ShippingService.Activities;

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
await primaryConnection.RegisterWorkflowActivityAsync<ShipOrder, OrderShipment>();

var secondaryConnection = await Connection.CreateInstanceAsync(new(new NATS.Client.Core.NatsOpts()
{
    Url = config.GetConnectionString("nats")!
})
{
    Namespace = Shared.Constants.SecondayNamespace
});
await secondaryConnection.RegisterWorkflowActivityAsync<ShipOrder, OrderShipment>();


sourceCancel.Token.WaitHandle.WaitOne();

await ((IAsyncDisposable)primaryConnection).DisposeAsync();
await ((IAsyncDisposable)secondaryConnection).DisposeAsync();