using NATS.Client.Core;
using Testcontainers.Nats;

namespace JetFlow.Testing.Helpers;

internal class NatsTestHarness : IAsyncDisposable
{
    private readonly NatsContainer container = new NatsBuilder("nats:latest")
            .WithCommand("-js") // enable JetStream"
            .Build();

    public NatsOpts Options { get; private set; } = default!;
    
    public async Task StartAsync()
    {
        await container.StartAsync();
        Options = new()
        {
            Url = container.GetConnectionString()
        };
    }

    public async ValueTask DisposeAsync()
    {
        await container.StopAsync();
    }
}
