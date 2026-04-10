using JetFlow;
using NATS.Client.Core;
using OpenTelemetry;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Sample;
using Sample.Activities;
using System.Diagnostics;

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

Console.WriteLine("Establishing Core Connection...");

var connection = await Connection.CreateInstanceAsync(new(
    new NatsOpts
    {
        Url = "nats://localhost:4222"
    }
));

Console.WriteLine("Registering workflow...");
await connection.RegisterWorkflowAsync<CreateUserWorkflow, User>();

Console.WriteLine("Registering activities...");
await connection.RegisterWorkflowActivityWithReturnAsync<DefineUsername,string,User>(new(), CancellationToken.None);
await connection.RegisterWorkflowActivityWithReturnAsync<IsUserUnique,bool>(new(), CancellationToken.None);

Console.WriteLine("Starting workflow...");
await connection.StartWorkflowAsync<CreateUserWorkflow, User>(new("Bob","Loblaw"), CancellationToken.None);

Console.WriteLine("Hit enter to exit...");
Console.ReadLine();
