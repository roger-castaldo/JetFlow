using JetFlow;
using NATS.Client.Core;

Console.WriteLine("Establishing Core Connection...");

var connection = await Connection.CreateInstanceAsync(new NatsOpts
{
    Url = "nats://localhost:4222"
});

Console.ReadLine();
