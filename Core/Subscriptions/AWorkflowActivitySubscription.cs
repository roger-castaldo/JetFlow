using JetFlow.Helpers;
using JetFlow.Messages;
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
        try
        {
            if (Equals(message.ActivityName, ActivityName))  
                await HandleEventAsync(message);
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
            if (Equals(message.ActivityName, ActivityName))
                await message.Message.AckAsync();
        }
    }

    private async ValueTask HandleEventAsync(EventMessage message)
    {
        if (await ActivityHelper.CanActivityRun<TWorkflowActivity>(timerStore, message, CancellationToken.None))
        {
            if (Equals(ActivityEventTypes.Start, message.ActivityEventType))
            {
                using var acitvity = TraceHelper.StartActivity(message);
                await HandleActivityRunAsync(await CreateState(message), message, CancellationToken.None);
            }
            else
                throw new InvalidOperationException($"Unsupported event type: {message.ActivityEventType}");
        }
    }

    private ValueTask<IWorkflowState> CreateState(EventMessage message)
        => new WorkflowState(natsJSContext, messageSerializer, message)
            .LoadAsync();

    protected abstract ValueTask HandleActivityRunAsync(IWorkflowState workflowState, EventMessage message, CancellationToken cancellationToken);
}
