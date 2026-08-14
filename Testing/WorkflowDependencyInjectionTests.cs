using JetFlow.Configs;
using JetFlow.Testing.Helpers;
using NATS.Client.Core;

namespace JetFlow.Testing;

[TestClass]
public class WorkflowDependencyInjectionTests
{
    private static NatsTestHarness? natsTestHarness;

    [ClassInitialize]
    public static async Task Init(TestContext testContext)
    {
        natsTestHarness = new NatsTestHarness();
        await natsTestHarness.StartAsync();
    }

    private sealed class DependentWorkflowWithInput : JetFlow.Interfaces.IWorkflow<string>
    {
        public DependentWorkflowWithInput(ICounterService counter)
        {
            counter.Increment();
        }

        public ValueTask ExecuteAsync(JetFlow.Interfaces.IWorkflowContext context, string? input)
            => ValueTask.CompletedTask;
    }

    [ClassCleanup]
    public static async Task Cleanup()
        => await (natsTestHarness?.DisposeAsync() ?? ValueTask.CompletedTask);

    private sealed class DependentWorkflow : JetFlow.Interfaces.IWorkflow
    {
        public DependentWorkflow(ICounterService counter)
        {
            counter.Increment();
        }

        public ValueTask ExecuteAsync(JetFlow.Interfaces.IWorkflowContext context)
            => ValueTask.CompletedTask;
    }

    [TestMethod]
    public async Task RegisterWorkflow_With_ServiceProvider_Resolves_Dependencies()
    {
        var (connection, counter) = await DIHelpers.CreateConnectionWithCounterAsync(natsTestHarness!);

        // Act: registering the workflow will create an instance via DI
        await connection.RegisterWorkflowAsync<DependentWorkflow>(cancellationToken: CancellationToken.None);

        // Assert: the DI-constructed workflow invoked the counter in its constructor
        Assert.AreEqual(1, counter.Count);

        await ((IAsyncDisposable)connection).DisposeAsync();
    }

    [TestMethod]
    public async Task RegisterWorkflow_With_FaultyServiceProvider_Throws()
    {
        Assert.IsNotNull(natsTestHarness);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var sp = new FaultyServiceProvider(typeof(ICounterService));

        var connectionOptions = new ConnectionOptions(natsConnection) { ServiceProvider = sp };
        var connection = await Connection.CreateInstanceAsync(connectionOptions);

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await connection.RegisterWorkflowAsync<DependentWorkflow>(cancellationToken: TestContext.CancellationToken));

        await ((IAsyncDisposable)connection).DisposeAsync();

        Assert.IsNotNull(ex);
    }

    [TestMethod]
    public async Task RegisterWorkflowWithInput_With_ServiceProvider_Resolves_Dependencies()
    {
        Assert.IsNotNull(natsTestHarness);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var counter = new CounterService();
        var sp = new SimpleServiceProvider(new Dictionary<Type, object?>() { [typeof(ICounterService)] = counter });

        var connectionOptions = new ConnectionOptions(natsConnection) { ServiceProvider = sp };
        var connection = await Connection.CreateInstanceAsync(connectionOptions);

        await connection.RegisterWorkflowAsync<DependentWorkflowWithInput, string>(cancellationToken: CancellationToken.None);

        Assert.AreEqual(1, counter.Count);

        await ((IAsyncDisposable)connection).DisposeAsync();
    }

    [TestMethod]
    public async Task RegisterWorkflowWithInput_When_ServiceProvider_MissingRegistration_Throws()
    {
        Assert.IsNotNull(natsTestHarness);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var sp = new SimpleServiceProvider(new Dictionary<Type, object?>());

        var connectionOptions = new ConnectionOptions(natsConnection) { ServiceProvider = sp };
        var connection = await Connection.CreateInstanceAsync(connectionOptions);

        var ex = await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () => await connection.RegisterWorkflowAsync<DependentWorkflowWithInput, string>(cancellationToken: CancellationToken.None));

        await ((IAsyncDisposable)connection).DisposeAsync();

        Assert.IsNotNull(ex);
    }

    [TestMethod]
    public async Task RegisterWorkflow_When_ServiceProvider_MissingRegistration_Throws()
    {
        var sp = new SimpleServiceProvider(new Dictionary<Type, object?>());
        var connection = await DIHelpers.CreateConnectionWithProviderAsync(natsTestHarness!, sp);

        try
        {
            await connection.RegisterWorkflowAsync<DependentWorkflow>(cancellationToken: CancellationToken.None);
            Assert.Fail("Expected workflow registration to throw when dependency is missing");
        }
        catch (Exception ex)
        {
            Assert.IsInstanceOfType(ex, typeof(InvalidOperationException));
        }

        await ((IAsyncDisposable)connection).DisposeAsync();
    }

    public TestContext TestContext { get; set; }
}
