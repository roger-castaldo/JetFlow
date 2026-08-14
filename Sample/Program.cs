using JetFlow;
using NATS.Client.Core;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sample;
using Sample.Activities;
using System.Text.Json;

var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource(Connection.TraceProviderName)
    .SetResourceBuilder(
        OpenTelemetry.Resources.ResourceBuilder.CreateDefault()
            .AddService(Connection.TraceProviderName)
    )
    .AddOtlpExporter(options =>
    {
        options.Endpoint = new("http://localhost:4317");
    })     // Optional: Export to OTLP endpoint
    .Build();

//var meterProvider = Sdk.CreateMeterProviderBuilder()
//    .AddMeter(Connection.MetricsMeterName)
//    .AddConsoleExporter()
//    .Build();

Console.WriteLine("Establishing Core Connection...");

var connection = await Connection.CreateInstanceAsync(new(
    new NatsOpts
    {
        Url = "nats://localhost:4222"
    }
));

Console.WriteLine("Registering workflow...");
await connection.RegisterWorkflowAsync<CreateUserWorkflow, User>(new()
{
    ErrorOnActivityTimeout = true,
    PurgeDelay = TimeSpan.FromSeconds(30),
    CompletionAction = JetFlow.Configs.WorkflowCompletionActions.ArchiveThenPurge
});

Console.WriteLine("Registering activities...");
await connection.RegisterWorkflowActivityWithReturnAsync<DefineUsername,string,User>(new(), CancellationToken.None);
await connection.RegisterWorkflowActivityWithReturnAsync<IsUserUnique,bool>(new(), CancellationToken.None);

var observer = await ObservationConnection.CreateInstanceAsync(new(
    new NatsOpts
    {
        Url = "nats://localhost:4222"
    }
));
await observer.AddDefaultNamespaceAsync();
await observer.AddPerformanceMonitoringAsync(
    1,
    async (workflowRecord) => {
        Console.WriteLine($"Workflow Performance: {JsonSerializer.Serialize(workflowRecord)}");
        Console.WriteLine($"Active Workflows: {await observer.GetActiveWorkflowCountAsync(null)}");
    },
    async (activityRecord) =>
    {
        Console.WriteLine($"Activity Performance: {JsonSerializer.Serialize(activityRecord)}");
        Console.WriteLine($"Active Activities: {await observer.GetActiveActivityCountAsync(null)}");
    }
);


Console.WriteLine("Starting workflows...");
await Task.WhenAll(new ValueTask<Guid>[]{
    connection.StartWorkflowAsync<CreateUserWorkflow, User>(new("Bob1","Loblaw1"), CancellationToken.None),
    connection.StartWorkflowAsync<CreateUserWorkflow, User>(new("Bob2", "Loblaw2"), CancellationToken.None),
    connection.StartWorkflowAsync<CreateUserWorkflow, User>(new("Bob3", "Loblaw3"), CancellationToken.None),
    connection.StartWorkflowAsync<CreateUserWorkflow, User>(new("Bob4", "Loblaw4"), CancellationToken.None),
    connection.StartWorkflowAsync<CreateUserWorkflow, User>(new("Bob5", "Loblaw5"), CancellationToken.None)
}.Select(vtask=>vtask.AsTask()));

Console.WriteLine("Hit enter to exit...");
Console.ReadLine();

await ((IAsyncDisposable)observer).DisposeAsync();
await ((IAsyncDisposable)connection).DisposeAsync();
