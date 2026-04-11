using JetFlow.Helpers;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;
using System.Diagnostics;

namespace JetFlow.Subscriptions;

internal abstract class AWorkflowActivitySubscription<TWorkflowActivity>(TWorkflowActivity instance,
    INatsConnection connection, INatsJSContext natsJSContext, INatsKVStore timerStore, 
    INatsJSConsumer consumer, MessageSerializer messageSerializer, CancellationToken cancellationToken)
    : ASubscription(consumer, cancellationToken)
{
    protected TWorkflowActivity Instance = instance;
    private string ActivityName = NameHelper.GetActivityName<TWorkflowActivity>();
    protected MessageSerializer MessageSerializer => messageSerializer;
    protected INatsConnection Connection => connection;

    protected override async ValueTask ProcessMessageAsync(EventMessage message)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        try
        {
            if (Equals(message.ActivityName, ActivityName))
            {
                if (!Equals(ActivityEventTypes.Start, message.ActivityEventType))
                    throw new InvalidOperationException($"Unsupported event type: {message.ActivityEventType}");
                var (canRun, aliveKey) = await ActivityHelper.CanActivityRun(timerStore, natsJSContext, message, CancellationToken.None);
                if (canRun)
                {
                    var start = MetricsHelper.StartActivity(message);
                    using var acitvity = TraceHelper.StartActivity(message);
                    var cancellationToken = new CancellationTokenSource();
                    Task.Run(async () =>
                    {
                        ulong currentRevision = 1;
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken.Token);
                            currentRevision = await ActivityHelper.KeepActivityAlive(timerStore, message, currentRevision, cancellationToken.Token);
                        }
                    });
                    await HandleActivityRunAsync(await CreateState(message), message, CancellationToken.None);
                    MetricsHelper.CompleteActivity(message, start);
                    await ActivityHelper.MarkActivityDoneInStore(timerStore, message, cancellationToken.Token);
                }
            }
            else
                await message.Message.NakAsync();
        }
        catch (WorkflowSuspendedException)
        {
            // handle workflow suspension by doing nothing
        }
        catch (Exception error)
        {
            Activity.Current?.SetStatus(ActivityStatusCode.Error, error.Message);
            await ActivityHelper.ErrorActivityAsync(message, error, connection, CancellationToken);
        }
        finally
        {
            await cancellationTokenSource.CancelAsync();
            if (Equals(message.ActivityName, ActivityName))
                await message.Message.AckAsync();
        }
    }

    private ValueTask<IWorkflowState> CreateState(EventMessage message)
        => new WorkflowState(natsJSContext, messageSerializer, message)
            .LoadAsync();

    protected abstract ValueTask HandleActivityRunAsync(IWorkflowState workflowState, EventMessage message, CancellationToken cancellationToken);
}
