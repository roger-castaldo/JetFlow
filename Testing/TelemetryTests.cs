using JetFlow.Configs;
using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Serializers;
using JetFlow.Testing.Helpers;
using NATS.Client.Core;
using System.Diagnostics;

namespace JetFlow.Testing;

[TestClass]
public class TelemetryTests
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

    private sealed class TimeoutActivity : IActivity<string>
    {
        async Task IActivity<string>.ExecuteAsync(string? input, IWorkflowState state, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        }
    }
    private sealed class NotImplementedActivity : IActivity
    {
        Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
    private sealed class WorkflowForTelemetry : IWorkflow
    {
        async ValueTask IWorkflow.ExecuteAsync(IWorkflowContext context)
        {
            _ = await context.ExecuteActivityAsync<TimeoutActivity, string>(new(TestsHelper.GenerateRandomString(32))
            {
                Timeouts=new(AttemptTimeout: TimeSpan.FromSeconds(3))
            });
            _ = await context.ExecuteActivityAsync<NotImplementedActivity>(new());
            await context.WaitAsync(TimeSpan.FromSeconds(3));
        }
    }

    [TestMethod]
    public async Task TestTelemetryGathering()
    {
        Assert.IsNotNull(natsTestHarness);
        //Arrange
        var runId = Guid.Empty;
        var name = NameHelper.GetWorkflowName<WorkflowForTelemetry>();
        var timeoutActivityName = NameHelper.GetActivityName<TimeoutActivity>();
        var notImplementedName = NameHelper.GetActivityName<NotImplementedActivity>();
        var capturedActivities = new List<Activity>();
        var listener = new ActivityListener()
        {
            ShouldListenTo = source => source.Name == Connection.TraceProviderName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStarted = _ => { },
            ActivityStopped = activity => {
                if(activity.Tags.Any(t=>Equals(t.Key,TraceConstants.WorkflowNameTag) && Equals(t.Value, name)))
                {
                    capturedActivities.Add(activity);
                }
            }
        };
        ActivitySource.AddActivityListener(listener);
        var subjectMapper = new SubjectMapper(null);
        var options = natsTestHarness.Options;
        var natsConnection = new NatsConnection(options);
        var connectionOptions = new ConnectionOptions(natsConnection);
        var connection = await Connection.CreateInstanceAsync(connectionOptions);
        await connection.RegisterWorkflowAsync<WorkflowForTelemetry>();
        await connection.RegisterWorkflowActivityAsync<TimeoutActivity,string>(new(), CancellationToken.None);
        await connection.RegisterWorkflowActivityAsync<NotImplementedActivity>(new());

        //Act
        var result = await WorkflowsHelper.StartWorkflowAndWaitForCompletion<WorkflowForTelemetry>(
            natsConnection,
            subjectMapper,
            async () =>
            {
                runId = await connection.StartWorkflowAsync<WorkflowForTelemetry>(CancellationToken.None);
                return runId;
            }
        );

        // Assert
        Assert.IsNotNull(result);
        await ((IAsyncDisposable)connection).DisposeAsync();
        //delay for otel completions
        await Task.Delay(TimeSpan.FromSeconds(5));

        Assert.HasCount(6, capturedActivities);
        var start = capturedActivities[0];
        Assert.AreEqual(TraceConstants.WorkflowStart, start.OperationName);
        Assert.AreEqual(ActivityKind.Internal, start.Kind);
        Assert.HasCount(2, start.Tags);
        Assert.IsTrue(start.Tags.All(t =>
            (Equals(t.Key, TraceConstants.WorkflowNameTag) && Equals(t.Value, name))
            || (Equals(t.Key, TraceConstants.WorkflowIdTag) && Equals(t.Value, runId.ToString()))
        ));
        Assert.HasCount(2, start.Events);
        Assert.IsTrue(start.Events.All(e =>
            Equals(e.Name, TraceConstants.MessagePublished)
            && (
                e.Tags.All(t=>Equals(t.Key, TraceConstants.MessageSubjectTag) && Equals(t.Value, subjectMapper.WorkflowConfigure(name, runId.ToString())))
                || e.Tags.All(t => Equals(t.Key, TraceConstants.MessageSubjectTag) && Equals(t.Value, subjectMapper.WorkflowStart(name, runId.ToString())))
            )
        ));

        var stepStart = capturedActivities[1];
        Assert.AreEqual(TraceConstants.WorkflowStepStart, stepStart.OperationName);
        Assert.AreEqual(ActivityKind.Producer, stepStart.Kind);
        Assert.HasCount(4, stepStart.Tags);
        Assert.IsTrue(stepStart.Tags.All(t =>
            (Equals(t.Key, TraceConstants.WorkflowNameTag) && Equals(t.Value, name))
            || (Equals(t.Key, TraceConstants.WorkflowIdTag) && Equals(t.Value, runId.ToString()))
            || (Equals(t.Key, TraceConstants.ActivityNameTag) && Equals(t.Value, timeoutActivityName))
            || Equals(t.Key, TraceConstants.ActivityIdTag)
        ));
        Assert.HasCount(2, stepStart.Events);
        Assert.IsTrue(stepStart.Events.All(e =>
            Equals(e.Name, TraceConstants.MessagePublished)
            && (
                e.Tags.All(t => Equals(t.Key, TraceConstants.MessageSubjectTag) && Equals(t.Value, subjectMapper.WorkflowStepStart(name, runId.ToString(), timeoutActivityName)))
                || e.Tags.All(t => Equals(t.Key, TraceConstants.MessageSubjectTag) && Equals(t.Value, subjectMapper.ActivityStart(timeoutActivityName, name, runId.ToString())))
            )
        ));
        Assert.HasCount(1, stepStart.Links);
        var lnk = stepStart.Links.First();
        Assert.AreEqual(start.SpanId, lnk.Context.SpanId);
        Assert.AreEqual(start.TraceId, lnk.Context.TraceId);

        var activityStart = capturedActivities[2];
        Assert.AreEqual(TraceConstants.WorkflowActivityStart, activityStart.OperationName);
        Assert.AreEqual(ActivityKind.Consumer, activityStart.Kind);
        Assert.AreEqual(ActivityStatusCode.Error, activityStart.Status);
        Assert.AreEqual("Activity execution timed out", activityStart.StatusDescription);
        Assert.AreEqual(name, activityStart.GetTagItem(TraceConstants.WorkflowNameTag));
        Assert.AreEqual(runId.ToString(), activityStart.GetTagItem(TraceConstants.WorkflowIdTag));
        Assert.AreEqual(timeoutActivityName, activityStart.GetTagItem(TraceConstants.ActivityNameTag));
        Assert.IsNotNull(activityStart.GetTagItem(TraceConstants.ActivityIdTag));
        Assert.IsNotNull(activityStart.GetTagItem(TraceConstants.ActivityTimeoutIdTag));
        Assert.HasCount(2, activityStart.Events);
        Assert.IsTrue(activityStart.Events.All(e =>
            (Equals(e.Name, TraceConstants.MessagePublished) && e.Tags.All(t => Equals(t.Key, TraceConstants.MessageSubjectTag) && Equals(t.Value, subjectMapper.WorkflowStepTimeout(name, runId.ToString(), timeoutActivityName))))
            || (Equals(e.Name, TraceConstants.MessageDecoded) && e.Tags.All(t => Equals(t.Key, TraceConstants.MessageContentHeaderTag) && Equals(t.Value, "application/json")))
        ));
        Assert.HasCount(1, activityStart.Links);
        lnk = activityStart.Links.First();
        Assert.AreEqual(stepStart.SpanId, lnk.Context.SpanId);
        Assert.AreEqual(stepStart.TraceId, lnk.Context.TraceId);

        stepStart = capturedActivities[3];
        Assert.AreEqual(TraceConstants.WorkflowStepStart, stepStart.OperationName);
        Assert.AreEqual(ActivityKind.Producer, stepStart.Kind);
        Assert.HasCount(4, stepStart.Tags);
        Assert.IsTrue(stepStart.Tags.All(t =>
            (Equals(t.Key, TraceConstants.WorkflowNameTag) && Equals(t.Value, name))
            || (Equals(t.Key, TraceConstants.WorkflowIdTag) && Equals(t.Value, runId.ToString()))
            || (Equals(t.Key, TraceConstants.ActivityNameTag) && Equals(t.Value, notImplementedName))
            || Equals(t.Key, TraceConstants.ActivityIdTag)
        ));
        Assert.HasCount(2, stepStart.Events);
        Assert.IsTrue(stepStart.Events.All(e =>
            Equals(e.Name, TraceConstants.MessagePublished)
            && (
                e.Tags.All(t => Equals(t.Key, TraceConstants.MessageSubjectTag) && Equals(t.Value, subjectMapper.WorkflowStepStart(name, runId.ToString(), notImplementedName)))
                || e.Tags.All(t => Equals(t.Key, TraceConstants.MessageSubjectTag) && Equals(t.Value, subjectMapper.ActivityStart(notImplementedName, name, runId.ToString())))
            )
        ));
        Assert.HasCount(1, stepStart.Links);
        lnk = stepStart.Links.First();
        Assert.AreEqual(start.SpanId, lnk.Context.SpanId);
        Assert.AreEqual(start.TraceId, lnk.Context.TraceId);

        activityStart = capturedActivities[4];
        Assert.AreEqual(TraceConstants.WorkflowActivityStart, activityStart.OperationName);
        Assert.AreEqual(ActivityKind.Consumer, activityStart.Kind);
        Assert.AreEqual(ActivityStatusCode.Error, activityStart.Status);
        Assert.AreEqual(new NotImplementedException().Message, activityStart.StatusDescription);
        Assert.AreEqual(name, activityStart.GetTagItem(TraceConstants.WorkflowNameTag));
        Assert.AreEqual(runId.ToString(), activityStart.GetTagItem(TraceConstants.WorkflowIdTag));
        Assert.AreEqual(notImplementedName, activityStart.GetTagItem(TraceConstants.ActivityNameTag));
        Assert.IsNotNull(activityStart.GetTagItem(TraceConstants.ActivityIdTag));
        Assert.HasCount(1, activityStart.Events);
        Assert.IsTrue(activityStart.Events.All(e =>
            Equals(e.Name, TraceConstants.MessagePublished) && e.Tags.All(t => Equals(t.Key, TraceConstants.MessageSubjectTag) && Equals(t.Value, subjectMapper.WorkflowStepError(name, runId.ToString(), notImplementedName)))
        ));
        Assert.HasCount(1, activityStart.Links);
        lnk = activityStart.Links.First();
        Assert.AreEqual(stepStart.SpanId, lnk.Context.SpanId);
        Assert.AreEqual(stepStart.TraceId, lnk.Context.TraceId);

        var delayActivity = capturedActivities[5];
        Assert.AreEqual(TraceConstants.WorkflowDelayStart, delayActivity.OperationName);
        Assert.AreEqual(ActivityKind.Producer, delayActivity.Kind);
        Assert.HasCount(2, delayActivity.Tags);
        Assert.IsTrue(delayActivity.Tags.All(t =>
            (Equals(t.Key, TraceConstants.WorkflowNameTag) && Equals(t.Value, name))
            || (Equals(t.Key, TraceConstants.WorkflowIdTag) && Equals(t.Value, runId.ToString()))
        ));
        Assert.HasCount(2, delayActivity.Events);
        Assert.IsTrue(delayActivity.Events.All(e =>
            Equals(e.Name, TraceConstants.MessagePublished)
            && (
                e.Tags.All(t => Equals(t.Key, TraceConstants.MessageSubjectTag) && Equals(t.Value, subjectMapper.WorkflowDelayStart(name, runId.ToString())))
                || e.Tags.All(t => Equals(t.Key, TraceConstants.MessageSubjectTag) && Equals(t.Value, subjectMapper.WorkflowTimer(name, runId.ToString())))
            )
        ));
        Assert.HasCount(1, delayActivity.Links);
        lnk = delayActivity.Links.First();
        Assert.AreEqual(start.SpanId, lnk.Context.SpanId);
        Assert.AreEqual(start.TraceId, lnk.Context.TraceId);
    }
}
