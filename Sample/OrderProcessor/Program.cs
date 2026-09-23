using JetFlow;
using Microsoft.Extensions.Configuration;
using OrderProcessor.Workflows;
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
await primaryConnection.RegisterWorkflowAsync<PlaceOrder, Order>(options: new() { CompletionAction = JetFlow.Configs.WorkflowCompletionActions.Archive, ErrorOnActivityFailure=true, ErrorOnActivityTimeout=true});

var secondaryConnection = await Connection.CreateInstanceAsync(new(new NATS.Client.Core.NatsOpts()
{
    Url = config.GetConnectionString("nats")!
})
{
    Namespace = Shared.Constants.SecondayNamespace
});
await secondaryConnection.RegisterWorkflowAsync<PlaceOrder, Order>(options: new() { CompletionAction = JetFlow.Configs.WorkflowCompletionActions.Archive, ErrorOnActivityFailure=true, ErrorOnActivityTimeout=true });


sourceCancel.Token.WaitHandle.WaitOne();

await ((IAsyncDisposable)primaryConnection).DisposeAsync();
await ((IAsyncDisposable)secondaryConnection).DisposeAsync();