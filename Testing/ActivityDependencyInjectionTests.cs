using JetFlow.Configs;
using JetFlow.Testing.Helpers;
using NATS.Client.Core;

namespace JetFlow.Testing;

[TestClass]
public class ActivityDependencyInjectionTests
{
    private static NatsTestHarness? natsTestHarness;

    [ClassInitialize]
    public static async Task Init(TestContext testContext)
    {
        natsTestHarness = new NatsTestHarness();
        await natsTestHarness.StartAsync();
    }
    // patch-fix: reapply context

    private sealed class DependentActivityWithInput : JetFlow.Interfaces.IActivity<string>
    {
        public DependentActivityWithInput(ICounterService counter)
        {
            counter.Increment();
        }

        public Task ExecuteAsync(JetFlow.Interfaces.IWorkflowState state, CancellationToken cancellationToken)
            => Task.CompletedTask;

        Task JetFlow.Interfaces.IActivity<string>.ExecuteAsync(string? input, JetFlow.Interfaces.IWorkflowState state, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    private sealed class DependentActivityWithReturn : JetFlow.Interfaces.IActivityWithReturn<string>
    {
        public DependentActivityWithReturn(ICounterService counter)
        {
            counter.Increment();
        }

        public Task<string> ExecuteAsync(JetFlow.Interfaces.IWorkflowState state, CancellationToken cancellationToken)
            => Task.FromResult("ok");
    }

    private sealed class DependentActivityWithReturnAndInput : JetFlow.Interfaces.IActivityWithReturn<string, string>
    {
        public DependentActivityWithReturnAndInput(ICounterService counter)
        {
            counter.Increment();
        }

        public Task<string> ExecuteAsync(string? input, JetFlow.Interfaces.IWorkflowState state, CancellationToken cancellationToken)
            => Task.FromResult(input ?? string.Empty);
    }

    [ClassCleanup]
    public static async Task Cleanup()
        => await (natsTestHarness?.DisposeAsync() ?? ValueTask.CompletedTask);

    private sealed class DependentActivity : JetFlow.Interfaces.IActivity
    {
        public DependentActivity(ICounterService counter)
        {
            counter.Increment();
        }

        public Task ExecuteAsync(JetFlow.Interfaces.IWorkflowState state, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }

    [TestMethod]
    public async Task RegisterActivity_With_ServiceProvider_Resolves_Dependencies()
    {
        var (connection, counter) = await DIHelpers.CreateConnectionWithCounterAsync(natsTestHarness!);

        // Act: registering the activity will create an instance via DI
        await connection.RegisterWorkflowActivityAsync<DependentActivity>(cancellationToken: CancellationToken.None);

        // Assert: the DI-constructed activity invoked the counter in its constructor
        Assert.AreEqual(1, counter.Count);

        await ((IAsyncDisposable)connection).DisposeAsync();
    }

    [TestMethod]
    public async Task RegisterActivity_With_FaultyServiceProvider_Throws()
    {
        Assert.IsNotNull(natsTestHarness);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        // Faulty provider that throws when resolving ICounterService
        var sp = new FaultyServiceProvider(typeof(ICounterService));

        var connectionOptions = new ConnectionOptions(natsConnection) { ServiceProvider = sp };
        var connection = await Connection.CreateInstanceAsync(connectionOptions);

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
        {
            await connection.RegisterWorkflowActivityAsync<DependentActivity>(cancellationToken: CancellationToken.None);
        });

        await ((IAsyncDisposable)connection).DisposeAsync();

        Assert.IsNotNull(ex);
    }

    [TestMethod]
    public async Task RegisterActivityWithInput_With_ServiceProvider_Resolves_Dependencies()
    {
        Assert.IsNotNull(natsTestHarness);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var counter = new CounterService();
        var sp = new SimpleServiceProvider(new Dictionary<Type, object?>() { [typeof(ICounterService)] = counter });

        var connectionOptions = new ConnectionOptions(natsConnection) { ServiceProvider = sp };
        var connection = await Connection.CreateInstanceAsync(connectionOptions);

        await connection.RegisterWorkflowActivityAsync<DependentActivityWithInput, string>(cancellationToken: CancellationToken.None);

        Assert.AreEqual(1, counter.Count);

        await ((IAsyncDisposable)connection).DisposeAsync();
    }

    [TestMethod]
    public async Task RegisterActivityWithReturn_With_ServiceProvider_Resolves_Dependencies()
    {
        Assert.IsNotNull(natsTestHarness);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var counter = new CounterService();
        var sp = new SimpleServiceProvider(new Dictionary<Type, object?>() { [typeof(ICounterService)] = counter });

        var connectionOptions = new ConnectionOptions(natsConnection) { ServiceProvider = sp };
        var connection = await Connection.CreateInstanceAsync(connectionOptions);

        await connection.RegisterWorkflowActivityWithReturnAsync<DependentActivityWithReturn, string>(cancellationToken: CancellationToken.None);

        Assert.AreEqual(1, counter.Count);

        await ((IAsyncDisposable)connection).DisposeAsync();
    }

    [TestMethod]
    public async Task RegisterActivityWithReturnAndInput_With_ServiceProvider_Resolves_Dependencies()
    {
        Assert.IsNotNull(natsTestHarness);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var counter = new CounterService();
        var sp = new SimpleServiceProvider(new Dictionary<Type, object?>() { [typeof(ICounterService)] = counter });

        var connectionOptions = new ConnectionOptions(natsConnection) { ServiceProvider = sp };
        var connection = await Connection.CreateInstanceAsync(connectionOptions);

        await connection.RegisterWorkflowActivityWithReturnAsync<DependentActivityWithReturnAndInput, string, string>(cancellationToken: CancellationToken.None);

        Assert.AreEqual(1, counter.Count);

        await ((IAsyncDisposable)connection).DisposeAsync();
    }

    [TestMethod]
    public async Task RegisterActivityWithInput_When_ServiceProvider_MissingRegistration_Throws()
    {
        Assert.IsNotNull(natsTestHarness);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var sp = new SimpleServiceProvider(new Dictionary<Type, object?>());

        var connectionOptions = new ConnectionOptions(natsConnection) { ServiceProvider = sp };
        var connection = await Connection.CreateInstanceAsync(connectionOptions);

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
        {
            await connection.RegisterWorkflowActivityAsync<DependentActivityWithInput, string>(cancellationToken: CancellationToken.None);
        });

        await ((IAsyncDisposable)connection).DisposeAsync();

        Assert.IsNotNull(ex);
    }

    [TestMethod]
    public async Task RegisterActivityWithReturn_When_ServiceProvider_MissingRegistration_Throws()
    {
        Assert.IsNotNull(natsTestHarness);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var sp = new SimpleServiceProvider(new Dictionary<Type, object?>());

        var connectionOptions = new ConnectionOptions(natsConnection) { ServiceProvider = sp };
        var connection = await Connection.CreateInstanceAsync(connectionOptions);

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
        {
            await connection.RegisterWorkflowActivityWithReturnAsync<DependentActivityWithReturn, string>(cancellationToken: CancellationToken.None);
        });

        await ((IAsyncDisposable)connection).DisposeAsync();

        Assert.IsNotNull(ex);
    }

    [TestMethod]
    public async Task RegisterActivityWithReturnAndInput_When_ServiceProvider_MissingRegistration_Throws()
    {
        Assert.IsNotNull(natsTestHarness);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var sp = new SimpleServiceProvider(new Dictionary<Type, object?>());

        var connectionOptions = new ConnectionOptions(natsConnection) { ServiceProvider = sp };
        var connection = await Connection.CreateInstanceAsync(connectionOptions);

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
        {
            await connection.RegisterWorkflowActivityWithReturnAsync<DependentActivityWithReturnAndInput, string, string>(cancellationToken: CancellationToken.None);
        });

        await ((IAsyncDisposable)connection).DisposeAsync();

        Assert.IsNotNull(ex);
    }

    [TestMethod]
    public async Task RegisterActivity_When_ServiceProvider_MissingRegistration_Throws()
    {
        var sp = new SimpleServiceProvider(new Dictionary<Type, object?>());
        var connection = await DIHelpers.CreateConnectionWithProviderAsync(natsTestHarness!, sp);

        try
        {
            await connection.RegisterWorkflowActivityAsync<DependentActivity>(cancellationToken: CancellationToken.None);
            Assert.Fail("Expected activity registration to throw when dependency is missing");
        }
        catch (Exception ex)
        {
            Assert.IsInstanceOfType(ex, typeof(InvalidOperationException));
        }

        await ((IAsyncDisposable)connection).DisposeAsync();
    }
}
