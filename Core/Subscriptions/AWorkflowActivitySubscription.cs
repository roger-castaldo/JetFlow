using JetFlow.Helpers;
using JetFlow.Interfaces;
using NATS.Client.JetStream;
using System.Diagnostics;

namespace JetFlow.Subscriptions;

internal abstract class AWorkflowActivitySubscription<TWorkflowActivity>(TWorkflowActivity instance,
    ServiceConnection serviceConnection, SubjectMapper subjectMapper, MessageSerializer messageSerializer,
    INatsJSConsumer consumer, CancellationToken cancellationToken)
    : ASubscription(serviceConnection, consumer, cancellationToken)
{
    protected TWorkflowActivity Instance = instance;
    private string ActivityName = NameHelper.GetActivityName<TWorkflowActivity>();
    protected MessageSerializer MessageSerializer => messageSerializer;

    protected override async ValueTask ProcessMessageAsync(EventMessage message)
    {
        using var cancellationTokenSource = new CancellationTokenSource();
        try
        {
            if (Equals(message.ActivityName, ActivityName))
            {
                if (!Equals(ActivityEventTypes.Start, message.ActivityEventType))
                    throw new InvalidOperationException($"Unsupported event type: {message.ActivityEventType}");
                var (canRun, aliveKey) = await ServiceConnection.CanActivityRun(message, CancellationToken.None);
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
                            currentRevision = await ServiceConnection.KeepActivityAlive(message, currentRevision, cancellationToken.Token);
                        }
                    });
                    await HandleActivityRunAsync(await WorkflowState.CreateAsync(ServiceConnection, messageSerializer, subjectMapper, message), message, CancellationToken.None);
                    MetricsHelper.CompleteActivity(message, start);
                    await ServiceConnection.MarkActivityDoneInStore(message, cancellationToken.Token);
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
            await RetryHelper.ProcessActivityRetryAsync(RetryTypes.Error, message, ServiceConnection, CancellationToken, error);
        }
        finally
        {
            await cancellationTokenSource.CancelAsync();
            if (Equals(message.ActivityName, ActivityName))
                await message.Message.AckAsync();
        }
    }
    protected abstract ValueTask HandleActivityRunAsync(IWorkflowState workflowState, EventMessage message, CancellationToken cancellationToken);
}
