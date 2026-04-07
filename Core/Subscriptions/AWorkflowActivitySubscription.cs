using JetFlow.Helpers;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;

namespace JetFlow.Subscriptions;

internal abstract class AWorkflowActivitySubscription<TWorkflowActivity>(TWorkflowActivity instance,
    INatsConnection connection, INatsJSContext natsJSContext, INatsKVStore timerStore, 
    INatsJSConsumer consumer, MessageSerializer messageSerializer, CancellationToken cancellationToken)
    : ASubscription(connection, natsJSContext, timerStore, messageSerializer, cancellationToken)
{
    protected TWorkflowActivity Instance = instance;

    protected override async Task StartStream(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await consumer.RefreshAsync(cancellationToken); // or try to recreate consumer

                await foreach (var msg in consumer.ConsumeAsync<byte[]>(cancellationToken: cancellationToken))
                {
                    (var activityName, var workflowName, var workflowId, var eventType) = SubjectHelper.ExtractActivityEventInfo(msg.Subject);
                    var activityId = ConnectionHelper.GetActivityID(msg);
                    try
                    {
                        if (Equals(activityName, NameHelper.GetActivityName<TWorkflowActivity>()))
                            await HandleEventAsync(activityName, workflowName, workflowId, activityId, eventType, msg);
                        else
                            await msg.NakAsync(cancellationToken: cancellationToken);
                    }
                    catch (WorkflowSuspendedException)
                    {
                        // handle workflow suspension by doing nothing
                    }
                    catch (Exception error)
                    {
                        await ActivityHelper.ErrorActivityAsync(workflowName, workflowId, activityName, activityId, error, Connection, cancellationToken);
                    }
                    finally
                    {
                        if (Equals(activityName, NameHelper.GetActivityName<TWorkflowActivity>()))
                            await msg.AckAsync(cancellationToken: cancellationToken);
                    }
                }
            }
            catch (NatsJSProtocolException e)
            {
                //bury error
            }
            catch (NatsJSException e)
            {
                // log exception
                await Task.Delay(1000, cancellationToken); // backoff
            }
        }
    }

    private async ValueTask HandleEventAsync(string activityName, string workflowName, string workflowId, Guid activityId, ActivityEventTypes eventType, INatsJSMsg<byte[]> msg)
    {
        if (await ActivityHelper.CanActivityRun<TWorkflowActivity>(TimerStore, workflowName, workflowId, CancellationToken.None))
            await (eventType switch { 
                ActivityEventTypes.Start => HandleActivityRunAsync(await CreateState(workflowName, workflowId, activityId), workflowName, workflowId, activityId, msg, CancellationToken.None),
                _ => throw new InvalidOperationException($"Unsupported event type: {eventType}")
            });
    }

    private ValueTask<IWorkflowState> CreateState(string workflowName, string workflowId, Guid activityId)
        => new WorkflowState(NatsJSContext, MessageSerializer, workflowName, workflowId, activityId)
            .LoadAsync();

    protected abstract ValueTask HandleActivityRunAsync(IWorkflowState workflowState, string workflowName, string workflowId, Guid activityId, INatsJSMsg<byte[]> msg, CancellationToken cancellationToken);
}
