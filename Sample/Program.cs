using JetFlow;
using NATS.Client.Core;
using Sample;
using Sample.Activities;

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
