using JetFlow.Configs;
using JetFlow.Data;
using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Serializers;
using JetFlow.Testing.Helpers;
using NATS.Client.Core;
using NATS.Client.JetStream;
using System.Numerics;

namespace JetFlow.Testing.Observation;

[TestClass]
public class CounterTests
{
    private static NatsTestHarness? natsTestHarness;

    [ClassInitialize]
    public static async Task Init(TestContext testContext)
    {
        natsTestHarness = new NatsTestHarness();
        await natsTestHarness.StartAsync();
    }

    [ClassCleanup]
    public static async Task Cleanup()
        => await (natsTestHarness?.DisposeAsync()??ValueTask.CompletedTask);

    private sealed class EmptyWorkflow(IObservationConnection observationConnection) : IWorkflow<string?>
    {
        async ValueTask IWorkflow<string?>.ExecuteAsync(IWorkflowContext context, string? input)
        {
            var counters = await observationConnection.GetActiveWorkflowCountAsync();
            Assert.IsNotEmpty(counters);
            Assert.HasCount(1, counters);
            var counter = counters.First();
            Assert.AreEqual(input, counter.Namespace);
            Assert.AreEqual(BigInteger.One, counter.Value);
            var counterValue = await observationConnection.GetActiveWorkflowCountAsync(input);
            Assert.AreEqual(BigInteger.One, counterValue);
            counters = await observationConnection.GetSuspendedWorkflowCountAsync();
            Assert.IsNotEmpty(counters);
            Assert.HasCount(1, counters);
            counter = counters.First();
            Assert.AreEqual(input, counter.Namespace);
            Assert.AreEqual(BigInteger.Zero, counter.Value);
            counterValue = await observationConnection.GetSuspendedWorkflowCountAsync(input);
            Assert.AreEqual(BigInteger.Zero, counterValue);
            await context.WaitAsync(TimeSpan.FromMinutes(3));
        }
    }

    [TestMethod]
    [DataRow(null, DisplayName = "Default namespace")]
    [DataRow("ensurecreation", DisplayName = "Custom namespace")]
    public async Task TestWorkflowCounters(string? namespaceValue)
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var subjectMapper = new SubjectMapper(namespaceValue);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var observationConnection = await ObservationConnection.CreateInstanceAsync(new(natsConnection));
        if (namespaceValue is null)
            await observationConnection.AddDefaultNamespaceAsync();
        else
            await observationConnection.AddNamespaceAsync(namespaceValue);

        var connectionOptions = new ConnectionOptions(natsConnection)
        {
            Namespace=namespaceValue,
            ServiceProvider=new SimpleServiceProvider(new Dictionary<Type, object?>([
                new(typeof(IObservationConnection), observationConnection)
            ]))
        };
        var messageSerializer = new MessageSerializer(connectionOptions.CompressionType, connectionOptions.JsonTypeInfoResolver);

        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<EmptyWorkflow, string?>(cancellationToken: CancellationToken.None);

        //Act
        var counters = await observationConnection.GetActiveWorkflowCountAsync();
        Assert.IsNotEmpty(counters);
        Assert.HasCount(1, counters);
        var counter = counters.First();
        Assert.AreEqual(namespaceValue, counter.Namespace);
        Assert.AreEqual(BigInteger.Zero, counter.Value);
        var counterValue = await observationConnection.GetActiveWorkflowCountAsync(namespaceValue);
        Assert.AreEqual(BigInteger.Zero, counterValue);
        Guid instance = Guid.Empty;
        var resultTask = WorkflowsHelper.StartWorkflowAndWaitForCompletion<EmptyWorkflow>(
            natsConnection,
            subjectMapper,
            async () =>
            {
                instance = await connection.StartWorkflowAsync<EmptyWorkflow, string?>(new(namespaceValue));
                return instance;
            }
        );

        await Task.Delay(TimeSpan.FromSeconds(30), TestContext.CancellationToken);

        var consumer = await new NatsJSContext(natsConnection).CreateConsumerAsync(subjectMapper.WorkflowEventsStreamsName, new() { 
            AckPolicy = NATS.Client.JetStream.Models.ConsumerConfigAckPolicy.All,
            FilterSubjects = [subjectMapper.WorkflowDelayStart(NameHelper.GetWorkflowName<EmptyWorkflow>(), instance.ToString())]
        }, TestContext.CancellationToken);

        var sub = consumer.FetchAsync<byte[]>(new() { MaxMsgs=1 }, cancellationToken: TestContext.CancellationToken);
        await foreach (var workflow in sub)
            break;
        counters = await observationConnection.GetSuspendedWorkflowCountAsync();
        Assert.IsNotEmpty(counters);
        Assert.HasCount(1, counters);
        counter = counters.First();
        Assert.AreEqual(namespaceValue, counter.Namespace);
        Assert.AreEqual(BigInteger.One, counter.Value);
        counterValue = await observationConnection.GetSuspendedWorkflowCountAsync(namespaceValue);
        Assert.AreEqual(BigInteger.One, counterValue);
        var result = await resultTask;

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsNotNull(result);
        var endMessage = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Data, result.Headers);
        Assert.IsNotNull(endMessage);
        Assert.IsTrue(endMessage.IsSuccess);

        //Verify
        counters = await observationConnection.GetActiveWorkflowCountAsync();
        Assert.IsNotEmpty(counters);
        Assert.HasCount(1, counters);
        counter = counters.First();
        Assert.AreEqual(namespaceValue, counter.Namespace);
        Assert.AreEqual(BigInteger.Zero, counter.Value);
        counterValue = await observationConnection.GetActiveWorkflowCountAsync(namespaceValue);
        Assert.AreEqual(BigInteger.Zero, counterValue);
        counters = await observationConnection.GetSuspendedWorkflowCountAsync();
        Assert.IsNotEmpty(counters);
        Assert.HasCount(1, counters);
        counter = counters.First();
        Assert.AreEqual(namespaceValue, counter.Namespace);
        Assert.AreEqual(BigInteger.Zero, counter.Value);
        counterValue = await observationConnection.GetSuspendedWorkflowCountAsync(namespaceValue);
        Assert.AreEqual(BigInteger.Zero, counterValue);
    }

    private sealed class EmptyActivity(IObservationConnection observationConnection) : IActivity<string?>
    {
        async Task IActivity<string?>.ExecuteAsync(string? input, IWorkflowState state, CancellationToken cancellationToken)
        {
            var counters = await observationConnection.GetActiveWorkflowCountAsync();
            Assert.IsNotEmpty(counters);
            Assert.HasCount(1, counters);
            var counter = counters.First();
            Assert.AreEqual(input, counter.Namespace);
            Assert.AreEqual(BigInteger.One, counter.Value);
            var counterValue = await observationConnection.GetActiveWorkflowCountAsync(input);
            Assert.AreEqual(BigInteger.One, counterValue);
            counters = await observationConnection.GetActiveActivityCountAsync();
            Assert.IsNotEmpty(counters);
            Assert.HasCount(1, counters);
            counter = counters.First();
            Assert.AreEqual(input, counter.Namespace);
            Assert.AreEqual(BigInteger.One, counter.Value);
            counterValue = await observationConnection.GetActiveActivityCountAsync(input);
            Assert.AreEqual(BigInteger.One, counterValue);
        }
    }
    private sealed class EmptyActivityWorkflow(IObservationConnection observationConnection) : IWorkflow<string?>
    {
        async ValueTask IWorkflow<string?>.ExecuteAsync(IWorkflowContext context, string? input)
        {
            var counters = await observationConnection.GetActiveActivityCountAsync();
            Assert.IsNotEmpty(counters);
            Assert.HasCount(1, counters);
            var counter = counters.First();
            Assert.AreEqual(input, counter.Namespace);
            Assert.AreEqual(BigInteger.Zero, counter.Value);
            var counterValue = await observationConnection.GetActiveActivityCountAsync(input);
            Assert.AreEqual(BigInteger.Zero, counterValue);
            await context.ExecuteActivityAsync<EmptyActivity, string>(new(input));
            counters = await observationConnection.GetActiveActivityCountAsync();
            Assert.IsNotEmpty(counters);
            Assert.HasCount(1, counters);
            counter = counters.First();
            Assert.AreEqual(input, counter.Namespace);
            Assert.AreEqual(BigInteger.Zero, counter.Value);
            counterValue = await observationConnection.GetActiveActivityCountAsync(input);
            Assert.AreEqual(BigInteger.Zero, counterValue);
        }
    }

    [TestMethod]
    [DataRow(null, DisplayName = "Default namespace")]
    [DataRow("ensurecreation", DisplayName = "Custom namespace")]
    public async Task TestActivityCounters(string? namespaceValue)
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var subjectMapper = new SubjectMapper(namespaceValue);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var observationConnection = await ObservationConnection.CreateInstanceAsync(new(natsConnection));
        if (namespaceValue is null)
            await observationConnection.AddDefaultNamespaceAsync();
        else
            await observationConnection.AddNamespaceAsync(namespaceValue);

        var connectionOptions = new ConnectionOptions(natsConnection)
        {
            Namespace=namespaceValue,
            ServiceProvider=new SimpleServiceProvider(new Dictionary<Type, object?>([
                new(typeof(IObservationConnection), observationConnection)
            ]))
        };
        var messageSerializer = new MessageSerializer(connectionOptions.CompressionType, connectionOptions.JsonTypeInfoResolver);

        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<EmptyActivityWorkflow, string?>(options: new() { CompletionAction=WorkflowCompletionActions.None, ErrorOnActivityFailure=true },cancellationToken: TestContext.CancellationToken);
        await connection.RegisterWorkflowActivityAsync<EmptyActivity, string>(cancellationToken: TestContext.CancellationToken);

        //Act
        var counters = await observationConnection.GetActiveActivityCountAsync();
        Assert.IsNotEmpty(counters);
        Assert.HasCount(1, counters);
        var counter = counters.First();
        Assert.AreEqual(namespaceValue, counter.Namespace);
        Assert.AreEqual(BigInteger.Zero, counter.Value);
        var counterValue = await observationConnection.GetActiveActivityCountAsync(namespaceValue);
        Assert.AreEqual(BigInteger.Zero, counterValue);
        counters = await observationConnection.GetActiveWorkflowCountAsync();
        Assert.IsNotEmpty(counters);
        Assert.HasCount(1, counters);
        counter = counters.First();
        Assert.AreEqual(namespaceValue, counter.Namespace);
        Assert.AreEqual(BigInteger.Zero, counter.Value);
        counterValue = await observationConnection.GetActiveWorkflowCountAsync(namespaceValue);
        Assert.AreEqual(BigInteger.Zero, counterValue);
        var result = await WorkflowsHelper.StartWorkflowAndWaitForCompletion<EmptyActivityWorkflow>(
            natsConnection,
            subjectMapper,
            async () => await connection.StartWorkflowAsync<EmptyActivityWorkflow, string?>(new(namespaceValue))
        );

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsNotNull(result);
        var endMessage = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Data, result.Headers);
        Assert.IsNotNull(endMessage);
        Assert.IsTrue(endMessage.IsSuccess);

        //Verify
        counters = await observationConnection.GetActiveActivityCountAsync();
        Assert.IsNotEmpty(counters);
        Assert.HasCount(1, counters);
        counter = counters.First();
        Assert.AreEqual(namespaceValue, counter.Namespace);
        Assert.AreEqual(BigInteger.Zero, counter.Value);
        counterValue = await observationConnection.GetActiveActivityCountAsync(namespaceValue);
        Assert.AreEqual(BigInteger.Zero, counterValue);
        counters = await observationConnection.GetActiveWorkflowCountAsync();
        Assert.IsNotEmpty(counters);
        Assert.HasCount(1, counters);
        counter = counters.First();
        Assert.AreEqual(namespaceValue, counter.Namespace);
        Assert.AreEqual(BigInteger.Zero, counter.Value);
        counterValue = await observationConnection.GetActiveWorkflowCountAsync(namespaceValue);
        Assert.AreEqual(BigInteger.Zero, counterValue);
    }

    public TestContext TestContext { get; set; }
}
