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
using System.Text.Json;

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

    private class UnregisteredActivity : IActivity
    {
        Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }

    private class WorkflowWithUnregisteredActivity : IWorkflow
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
            () => connection.StartWorkflowAsync<WorkflowWithUnregisteredActivity>(CancellationToken.None)
        );

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsTrue(result.HasValue);
        var endResult = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Value.Data, result.Value.Headers);

        //Verify
        Assert.IsNotNull(endResult);
        Assert.IsFalse(endResult.IsSuccess);
        Assert.AreEqual($"Activity {NameHelper.GetActivityName<UnregisteredActivity>()} has timed out", endResult.ErrorMessage);
    }

    private class SlowActivity : IActivity
    {
        async Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }

    private class WorkflowWithSlowActivity : IWorkflow
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
            () => connection.StartWorkflowAsync<WorkflowWithSlowActivity>(CancellationToken.None)
        );

        // Assert
        await ((IAsyncDisposable)connection).DisposeAsync();
        Assert.IsTrue(result.HasValue);
        var endResult = await messageSerializer.DecodeAsync<WorkflowEnd>(result.Value.Data, result.Value.Headers);

        //Verify
        Assert.IsNotNull(endResult);
        Assert.IsFalse(endResult.IsSuccess);
        Assert.AreEqual($"Activity {NameHelper.GetActivityName<SlowActivity>()} has timed out", endResult.ErrorMessage);
    }

    private class WorkflowWithNoSteps : IWorkflow
    {
        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }

    private record TimeStampResult(byte[]? Message, long Timestamp);

    private async Task<(TimeStampResult? completion, TimeStampResult? archive, TimeStampResult? purge)> ExecuteCompletionTest(WorkflowCompletionActions completionAction, TimeSpan? purgeDelay=null)
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var cancellationTokenSource = new CancellationTokenSource();
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection);
        var messageSerializer = new MessageSerializer(connectionOptions);
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
                await foreach (var msg in natsConnection.SubscribeAsync<byte[]>(subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<WorkflowWithNoSteps>(), "*"), cancellationToken: cancellationTokenSource.Token))
                {
                    if (Equals(msg.Subject, subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<WorkflowWithNoSteps>(), runId.ToString())))
                    {
                        completion.TrySetResult(new(msg.Data,Stopwatch.GetTimestamp()));
                        break;
                    }
                }
            }
            catch (OperationCanceledException) { }
        });
        _ = Task.Run(async () =>
        {
            try
            {
                await foreach (var msg in natsConnection.SubscribeAsync<byte[]>(subjectMapper.WorkflowArchived(NameHelper.GetWorkflowName<WorkflowWithNoSteps>(), "*"), cancellationToken: cancellationTokenSource.Token))
                {
                    if (Equals(msg.Subject, subjectMapper.WorkflowArchived(NameHelper.GetWorkflowName<WorkflowWithNoSteps>(), runId.ToString())))
                    {
                        archive.TrySetResult(new(msg.Data, Stopwatch.GetTimestamp()));
                        break;
                    }
                }
            }
            catch (OperationCanceledException) { }
        });
        _ = Task.Run(async () =>
        {
            try
            {
                if (purgeDelay.HasValue)
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
                else
                {
                    await foreach (var msg in natsConnection.SubscribeAsync<byte[]>(subjectMapper.WorkflowPurge(NameHelper.GetWorkflowName<WorkflowWithNoSteps>(), "*"), cancellationToken: cancellationTokenSource.Token))
                    {
                        if (Equals(msg.Subject, subjectMapper.WorkflowPurge(NameHelper.GetWorkflowName<WorkflowWithNoSteps>(), runId.ToString())))
                        {
                            purge.TrySetResult(new(msg.Data, Stopwatch.GetTimestamp()));
                            break;
                        }
                    }
                }
            }
            catch (OperationCanceledException) { }
        });

        //Act
        runId = await connection.StartWorkflowAsync<WorkflowWithNoSteps>(cancellationTokenSource.Token);

        //Assert
        var completionResult = await completion.Task;
        var archiveResult = await (await Task.WhenAny<TimeStampResult?>(
            archive.Task,
            Task.Delay(TimeSpan.FromSeconds(10)).ContinueWith<TimeStampResult?>(_ => null)
        ));
        var purgeResult = await (await Task.WhenAny<TimeStampResult?>(
            purge.Task,
            Task.Delay(TimeSpan.FromSeconds(10)).ContinueWith<TimeStampResult?>(_ => null)
        ));

        //cleanup
        await cancellationTokenSource.CancelAsync();
        await ((IAsyncDisposable)connection).DisposeAsync();

        return (completionResult, archiveResult, purgeResult);
    }

    [TestMethod]
    public async Task WorkflowCompletionPostActionNone()
    {
        //Act
        var results = await ExecuteCompletionTest(WorkflowCompletionActions.None);

        //Assert
        Assert.IsNotNull(results.completion);
        Assert.IsNull(results.archive);
        Assert.IsNull(results.purge);
    }

    [TestMethod]
    public async Task WorkflowCompletionPostActionArchiveThenNothing()
    {
        //Act
        var results = await ExecuteCompletionTest(WorkflowCompletionActions.ArchiveThenNothing);

        //Assert
        Assert.IsNotNull(results.completion);
        Assert.IsNotNull(results.archive);
        Assert.IsNull(results.purge);
        Assert.IsGreaterThan(results.completion.Timestamp, results.archive.Timestamp);
    }

    [TestMethod]
    public async Task WorkflowCompletionPostActionArchiveThenPurge()
    {
        //Act
        var results = await ExecuteCompletionTest(WorkflowCompletionActions.ArchiveThenPurge);

        //Assert
        Assert.IsNotNull(results.completion);
        Assert.IsNotNull(results.archive);
        Assert.IsNotNull(results.purge);
        Assert.IsGreaterThan(results.completion.Timestamp, results.archive.Timestamp);
        Assert.IsGreaterThan(results.archive.Timestamp, results.purge.Timestamp);
    }

    [TestMethod]
    public async Task WorkflowCompletionPostActionPurge()
    {
        //Act
        var results = await ExecuteCompletionTest(WorkflowCompletionActions.Purge);

        //Assert
        Assert.IsNotNull(results.completion);
        Assert.IsNull(results.archive);
        Assert.IsNotNull(results.purge);
        Assert.IsGreaterThan(results.completion.Timestamp, results.purge.Timestamp);
    }

    [TestMethod]
    [DataRow(WorkflowCompletionActions.ArchiveThenPurge)]
    [DataRow(WorkflowCompletionActions.Purge)]
    public async Task WorkflowCompletionPurgeWithDelay(WorkflowCompletionActions completionAction)
    {
        //Arrange
        var delay = TimeSpan.FromSeconds(RandomNumberGenerator.GetInt32(1,5));
        //Act
        var results = await ExecuteCompletionTest(completionAction, delay);

        //Assert
        Assert.IsNotNull(results.completion);
        Assert.IsNotNull(results.purge);
        double mid = double.MaxValue;
        if (completionAction== WorkflowCompletionActions.ArchiveThenPurge)
        {
            Assert.IsNotNull(results.archive);
            mid = Math.Floor(TimeSpan.FromTicks(results.purge.Timestamp-results.archive.Timestamp).TotalSeconds);
        }
        else
        {
            Assert.IsNull(results.archive);
            mid = Math.Floor(TimeSpan.FromTicks(results.purge.Timestamp-results.completion.Timestamp).TotalSeconds);
        }
        Assert.IsLessThanOrEqualTo(mid+1, Math.Floor(delay.TotalSeconds));
        Assert.IsGreaterThanOrEqualTo(mid-1, Math.Floor(delay.TotalSeconds));
    }
}
