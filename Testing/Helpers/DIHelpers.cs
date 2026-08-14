using NATS.Client.Core;
using JetFlow.Configs;
using JetFlow.Interfaces;

namespace JetFlow.Testing.Helpers;

internal static class DIHelpers
{
    public static async Task<(IConnection connection, CounterService counter)> CreateConnectionWithCounterAsync(NatsTestHarness harness)
    {
        var options = harness.Options;
        var natsConnection = new NatsConnection(options);
        var counter = new CounterService();
        var sp = new SimpleServiceProvider(new Dictionary<Type, object?>() { [typeof(ICounterService)] = counter });

        var connectionOptions = new ConnectionOptions(natsConnection) { ServiceProvider = sp };
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        return (connection, counter);
    }

    public static async Task<IConnection> CreateConnectionWithProviderAsync(NatsTestHarness harness, IServiceProvider serviceProvider)
    {
        var options = harness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection) { ServiceProvider = serviceProvider };
        return await Connection.CreateInstanceAsync(connectionOptions);
    }
}

internal interface ICounterService { void Increment(); int Count { get; } }
internal sealed class CounterService : ICounterService { public int Count { get; private set; } = 0; public void Increment() => Count++; }
