using JetFlow;
using NATS.Client.Core;
using Sample;

Console.WriteLine("Establishing Core Connection...");

var connection = await Connection.CreateInstanceAsync(new NatsOpts
{
    Url = "nats://localhost:4222"
});

Console.WriteLine("Registering workflow...");
await connection.RegisterWorkflowAsync<CreateUserWorkflow, User>(CancellationToken.None);

Console.WriteLine("Starting workflow...");
await connection.StartWorkflowAsync<CreateUserWorkflow, User>(new("Bob","Loblaw"), CancellationToken.None);

Console.WriteLine("Hit enter to exit...");
Console.ReadLine();
