using JetFlow.Configs;
using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Messages;
using JetFlow.Serializers;
using JetFlow.Testing.Helpers;
using NATS.Client.Core;
using NATS.Client.JetStream;
using System.Diagnostics;
using System.Security.Cryptography;

namespace JetFlow.Testing;

[TestClass]
public class WorkflowOptionTests
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

    private sealed class UnregisteredActivity : IActivity
    {
        Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class WorkflowWithUnregisteredActivity : IWorkflow
    {
        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            await context.ExecuteActivityAsync<UnregisteredActivity>(new() { Timeouts = new(OverallTimeout:TimeSpan.FromSeconds(5))});
        }
    }

    [TestMethod]
    public async Task TestErrorOnActivityStartTimeout()
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection);
        var messageSerializer = new MessageSerializer(connectionOptions);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<WorkflowWithUnregisteredActivity>(new() { ErrorOnActivityTimeout=true }, CancellationToken.None);

        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForCompletion<WorkflowWithUnregisteredActivity>(natsConnection, subjectMapper,
            async () => await connection.StartWorkflowAsync<WorkflowWithUnregisteredActivity>(CancellationToken.None)
        );

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsNotNull(result);
        var endResult = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Data, result.Headers);

        //Verify
        Assert.IsNotNull(endResult);
        Assert.IsFalse(endResult.IsSuccess);
        Assert.AreEqual($"Activity {NameHelper.GetActivityName<UnregisteredActivity>()} has timed out", endResult.ErrorMessage);
    }

    private sealed class SlowActivity : IActivity
    {
        async Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
        }
    }

    private sealed class WorkflowWithSlowActivity : IWorkflow
    {
        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            await context.ExecuteActivityAsync<SlowActivity>(new() { Timeouts=new(AttemptTimeout: TimeSpan.FromSeconds(5)) });
        }
    }

    [TestMethod]
    public async Task TestErrorOnActivityRunTimeout()
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection);
        var messageSerializer = new MessageSerializer(connectionOptions);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<WorkflowWithSlowActivity>(new() { ErrorOnActivityTimeout=true }, CancellationToken.None);
        await connection.RegisterWorkflowActivityAsync<SlowActivity>(new(), CancellationToken.None);

        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForCompletion<WorkflowWithSlowActivity>(natsConnection, subjectMapper,
            async () => await connection.StartWorkflowAsync<WorkflowWithSlowActivity>(CancellationToken.None)
        );

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsNotNull(result);
        var endResult = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Data, result.Headers);

        //Verify
        Assert.IsNotNull(endResult);
        Assert.IsFalse(endResult.IsSuccess);
        Assert.AreEqual($"Activity {NameHelper.GetActivityName<SlowActivity>()} has timed out", endResult.ErrorMessage);
    }

    private sealed class ActivityThatThrowsAnError : IActivity
    {
        public Task ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class WorkflowWithActivityError : IWorkflow
    {
        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            _ = await context.ExecuteActivityAsync<ActivityThatThrowsAnError>(new());
        }
    }

    [TestMethod]
    public async Task TestErrorOnActivityThrowsException()
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection);
        var messageSerializer = new MessageSerializer(connectionOptions);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<WorkflowWithActivityError>(new() { ErrorOnActivityFailure=true }, CancellationToken.None);
        await connection.RegisterWorkflowActivityAsync<ActivityThatThrowsAnError>(new(), CancellationToken.None);

        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForCompletion<WorkflowWithActivityError>(natsConnection, subjectMapper,
            async () => await connection.StartWorkflowAsync<WorkflowWithActivityError>(CancellationToken.None)
        );

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsNotNull(result);
        var endResult = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Data, result.Headers);

        //Verify
        Assert.IsNotNull(endResult);
        Assert.IsFalse(endResult.IsSuccess);
        Assert.AreEqual($"Activity {NameHelper.GetActivityName<ActivityThatThrowsAnError>()} has failed with error: {new NotImplementedException().Message}", endResult.ErrorMessage);
    }

    private sealed class WorkflowWithNoSteps : IWorkflow
    {
        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }

    private sealed record TimeStampResult(byte[]? Message, long Timestamp);

    private static async Task<(TimeStampResult? completion, TimeStampResult? archive, TimeStampResult? purge)> ExecuteCompletionTest(WorkflowCompletionActions completionAction, TimeSpan? purgeDelay=null)
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<WorkflowWithNoSteps>(new() { CompletionAction = completionAction, PurgeDelay= purgeDelay }, CancellationToken.None);

        var runId = Guid.Empty;
        var completion = new TaskCompletionSource<TimeStampResult?>();
        var archive = new TaskCompletionSource<TimeStampResult?>();
        var purge = new TaskCompletionSource<TimeStampResult?>();

        _ = Task.Run(async () =>
        {
            try
            {
                var context = new NatsJSContext(natsConnection);
                var consumer = await context.CreateConsumerAsync(
                    subjectMapper.WorkflowEventsStreamsName,
                    new(Guid.NewGuid().ToString())
                    {
                        FilterSubject=subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<WorkflowWithNoSteps>(), "*"),
                        AckPolicy = NATS.Client.JetStream.Models.ConsumerConfigAckPolicy.None
                    }
                );
                await consumer.RefreshAsync(cancellationTokenSource.Token);
                await foreach (var msg in consumer.ConsumeAsync<byte[]>(cancellationToken: cancellationTokenSource.Token))
                {
                    if (Equals(msg.Subject, subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<WorkflowWithNoSteps>(), runId.ToString())))
                    {
                        completion.TrySetResult(new(msg.Data,Stopwatch.GetTimestamp()));
                        break;
                    }
                }
                await context.DeleteConsumerAsync(subjectMapper.WorkflowEventsStreamsName, consumer.Info.Name);
            }
            catch (OperationCanceledException) { /*buried to handle cancelling*/}
        });
        _ = Task.Run(async () =>
        {
            try
            {
                var context = new NatsJSContext(natsConnection);
                var consumer = await context.CreateConsumerAsync(
                    subjectMapper.WorkflowEventsStreamsName,
                    new(Guid.NewGuid().ToString())
                    {
                        FilterSubject=subjectMapper.WorkflowArchived(NameHelper.GetWorkflowName<WorkflowWithNoSteps>(), "*"),
                        AckPolicy = NATS.Client.JetStream.Models.ConsumerConfigAckPolicy.None
                    }
                );
                await consumer.RefreshAsync(cancellationTokenSource.Token);
                await foreach (var msg in consumer.ConsumeAsync<byte[]>(cancellationToken: cancellationTokenSource.Token))
                {
                    if (Equals(msg.Subject, subjectMapper.WorkflowArchived(NameHelper.GetWorkflowName<WorkflowWithNoSteps>(), runId.ToString())))
                    {
                        archive.TrySetResult(new(msg.Data, Stopwatch.GetTimestamp()));
                        break;
                    }
                }
                await context.DeleteConsumerAsync(subjectMapper.WorkflowEventsStreamsName, consumer.Info.Name);
            }
            catch (OperationCanceledException) {/*buried to handle cancelling*/ }
        });
        _ = Task.Run(async () =>
        {
            try
            {
                var context = new NatsJSContext(natsConnection);
                var consumer = await context.CreateConsumerAsync(
                    subjectMapper.WorkflowEventsStreamsName,
                    new(Guid.NewGuid().ToString())
                    {
                        FilterSubject=subjectMapper.WorkflowPurge(NameHelper.GetWorkflowName<WorkflowWithNoSteps>(), "*"),
                        AckPolicy = NATS.Client.JetStream.Models.ConsumerConfigAckPolicy.None
                    }
                );
                await consumer.RefreshAsync(cancellationTokenSource.Token);
                await foreach (var msg in consumer.ConsumeAsync<byte[]>(cancellationToken: cancellationTokenSource.Token))
                {
                    if (Equals(msg.Subject, subjectMapper.WorkflowPurge(NameHelper.GetWorkflowName<WorkflowWithNoSteps>(), runId.ToString())))
                    {
                        purge.TrySetResult(new(msg.Data, Stopwatch.GetTimestamp()));
                        break;
                    }
                }
                await context.DeleteConsumerAsync(subjectMapper.WorkflowEventsStreamsName, consumer.Info.Name);
            }
            catch (OperationCanceledException) { /*buried to handle cancelling*/}
        });

        //Act
        runId = await connection.StartWorkflowAsync<WorkflowWithNoSteps>(cancellationTokenSource.Token);

        //Assert
        var completionResult = await completion.Task;
        var archiveResult = await (await Task.WhenAny<TimeStampResult?>(
            archive.Task,
            Task.Delay(TimeSpan.FromSeconds(20)).ContinueWith<TimeStampResult?>(_ => null)
        ));
        var purgeResult = await (await Task.WhenAny<TimeStampResult?>(
            purge.Task,
            Task.Delay(TimeSpan.FromSeconds(20)).ContinueWith<TimeStampResult?>(_ => null)
        ));

        //cleanup
        await cancellationTokenSource.CancelAsync();
        if (purgeResult!=null)
            await Task.Delay(TimeSpan.FromSeconds(30)); //wait for any in-flight messages to be processed before disposing connection
        await ((IAsyncDisposable)connection).DisposeAsync();

        return (completionResult, archiveResult, purgeResult);
    }

    [TestMethod]
    public async Task WorkflowCompletionPostActionNone()
    {
        //Act
        var (completion, archive, purge)= await WorkflowOptionTests.ExecuteCompletionTest(WorkflowCompletionActions.None);

        //Assert
        Assert.IsNotNull(completion);
        Assert.IsNull(archive);
        Assert.IsNull(purge);
    }

    [TestMethod]
    public async Task WorkflowCompletionPostActionArchiveThenNothing()
    {
        //Act
        var (completion, archive, purge)= await WorkflowOptionTests.ExecuteCompletionTest(WorkflowCompletionActions.ArchiveThenNothing);

        //Assert
        Assert.IsNotNull(completion);
        Assert.IsNotNull(archive);
        Assert.IsNull(purge);
        Assert.IsGreaterThan(completion.Timestamp, archive.Timestamp);
    }

    [TestMethod]
    public async Task WorkflowCompletionPostActionArchiveThenPurge()
    {
        //Act
        var (completion, archive, purge)= await WorkflowOptionTests.ExecuteCompletionTest(WorkflowCompletionActions.ArchiveThenPurge, purgeDelay: TimeSpan.FromSeconds(1));

        //Assert
        Assert.IsNotNull(completion);
        Assert.IsNotNull(archive);
        Assert.IsNotNull(purge);
        Assert.IsGreaterThanOrEqualTo(completion.Timestamp, archive.Timestamp);
        Assert.IsGreaterThanOrEqualTo(archive.Timestamp, purge.Timestamp);
    }

    [TestMethod]
    public async Task WorkflowCompletionPostActionPurge()
    {
        //Act
        var (completion, archive, purge)= await WorkflowOptionTests.ExecuteCompletionTest(WorkflowCompletionActions.Purge);

        //Assert
        Assert.IsNotNull(completion);
        Assert.IsNull(archive);
        Assert.IsNotNull(purge);
        Assert.IsGreaterThan(completion.Timestamp, purge.Timestamp);
    }

    [TestMethod]
    [DataRow(WorkflowCompletionActions.ArchiveThenPurge)]
    [DataRow(WorkflowCompletionActions.Purge)]
    public async Task WorkflowCompletionPurgeWithDelay(WorkflowCompletionActions completionAction)
    {
        //Arrange
        var delay = TimeSpan.FromSeconds(RandomNumberGenerator.GetInt32(3,5));
        //Act
        var (completion, archive, purge)= await WorkflowOptionTests.ExecuteCompletionTest(completionAction, delay);

        //Assert
        Assert.IsNotNull(completion);
        Assert.IsNotNull(purge);
        double mid;
        if (completionAction== WorkflowCompletionActions.ArchiveThenPurge)
        {
            Assert.IsNotNull(archive);
            mid = Math.Floor(Stopwatch.GetElapsedTime(archive.Timestamp).Subtract(Stopwatch.GetElapsedTime(purge.Timestamp)).TotalSeconds);
        }
        else
        {
            Assert.IsNull(archive);
            mid = Math.Floor(Stopwatch.GetElapsedTime(completion.Timestamp).Subtract(Stopwatch.GetElapsedTime(purge.Timestamp)).TotalSeconds);
        }
        Assert.IsGreaterThanOrEqualTo(delay.TotalSeconds-1, mid);
        Assert.IsLessThanOrEqualTo(delay.TotalSeconds+1, mid);
    }
}
