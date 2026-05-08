using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Serializers;
using NATS.Client.JetStream;
using System.Diagnostics;

namespace JetFlow.Subscriptions;

internal abstract class AWorkflowActivitySubscription<TWorkflowActivity>(TWorkflowActivity instance,
    ServiceConnection serviceConnection, SubjectMapper subjectMapper, MessageSerializer messageSerializer,
    INatsJSConsumer consumer, CancellationToken cancellationToken)
    : ASubscription(serviceConnection, consumer, cancellationToken)
{
    protected TWorkflowActivity Instance = instance;
    private readonly string ActivityName = NameHelper.GetActivityName<TWorkflowActivity>();
    protected MessageSerializer MessageSerializer => messageSerializer;

    protected override async ValueTask ProcessMessageAsync(EventMessage message)
    {
        using var activityKeepaliveCTS = new CancellationTokenSource();
        using var timeoutCts = (message.ActivityTimeout.HasValue ? new CancellationTokenSource(message.ActivityTimeout.Value) : null);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            CancellationToken,
            timeoutCts?.Token??CancellationToken.None);
        var ackMessage = true;
        Activity? activity = null;
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
                    activity = TraceHelper.StartActivity(message);
                    Task.Run(async () =>
                    {
                        ulong currentRevision = 1;
                        while (!activityKeepaliveCTS.IsCancellationRequested)
                        {
                            await Task.Delay(TimeSpan.FromMinutes(1), activityKeepaliveCTS.Token);
                            currentRevision = await ServiceConnection.KeepActivityAlive(message, currentRevision, activityKeepaliveCTS.Token);
                        }
                    });
                    await HandleActivityRunAsync(await WorkflowState.CreateAsync(ServiceConnection, messageSerializer, subjectMapper, message), message, linkedCts.Token)
                        .WaitAsync(linkedCts.Token);
                    MetricsHelper.CompleteActivity(message, start);
                    await ServiceConnection.MarkActivityDoneInStore(message, CancellationToken);
                }
            }
            else
                ackMessage=false;
        }
        catch (WorkflowSuspendedException){ /* handle workflow suspension by doing nothing */}
        catch (OperationCanceledException) when (timeoutCts?.IsCancellationRequested??false)
        {
            // timed out
            TraceHelper.AddActivityTimeout(message.ActivityTimeout!.Value);
            Activity.Current?.SetStatus(ActivityStatusCode.Error, "Activity execution timed out");
            await RetryHelper.ProcessActivityRetryAsync(RetryTypes.Timeout, message, ServiceConnection, CancellationToken);
        }
        catch (OperationCanceledException) when (CancellationToken.IsCancellationRequested)
        {
            // caller explicitly canceled
            ackMessage=false;
        }
        catch (Exception error)
        {
            Activity.Current?.SetStatus(ActivityStatusCode.Error, error.Message);
            await RetryHelper.ProcessActivityRetryAsync(RetryTypes.Error, message, ServiceConnection, CancellationToken, error);
        }
        finally
        {
            try
            {
                await activityKeepaliveCTS.CancelAsync();
            }
            catch { /* burying error in case cancellation fails*/ }
            activity?.Dispose();
            if (ackMessage)
                await message.Message.AckAsync();
            else
                await message.Message.NakAsync();
        }
    }
    protected abstract Task HandleActivityRunAsync(IWorkflowState workflowState, EventMessage message, CancellationToken cancellationToken);
}
