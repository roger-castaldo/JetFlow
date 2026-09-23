using JetFlow;
using Microsoft.Extensions.Configuration;
using PaymentService.Activities;
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
await primaryConnection.RegisterWorkflowActivityWithReturnAsync<ProcessPayment, bool, PaymentRequest>();
await primaryConnection.RegisterWorkflowActivityAsync<RefundPayment, PaymentRequest>();

var secondaryConnection = await Connection.CreateInstanceAsync(new(new NATS.Client.Core.NatsOpts()
{
    Url = config.GetConnectionString("nats")!
})
{
    Namespace = Shared.Constants.SecondayNamespace
});
await secondaryConnection.RegisterWorkflowActivityWithReturnAsync<ProcessPayment, bool, PaymentRequest>();
await secondaryConnection.RegisterWorkflowActivityAsync<RefundPayment, PaymentRequest>();


sourceCancel.Token.WaitHandle.WaitOne();

await ((IAsyncDisposable)primaryConnection).DisposeAsync();
await ((IAsyncDisposable)secondaryConnection).DisposeAsync();