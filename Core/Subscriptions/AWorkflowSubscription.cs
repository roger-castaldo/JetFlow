using JetFlow.Helpers;
using JetFlow.Messages;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;
using System.Diagnostics;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JetFlow.Subscriptions;

internal abstract class AWorkflowSubscription<TWorkflow>(INatsConnection connection, INatsJSContext natsJSContext, INatsKVStore timerStore, INatsJSConsumer consumer, 
    MessageSerializer messageSerializer, CancellationToken cancellationToken)
    : ASubscription(consumer, cancellationToken)
    where TWorkflow : class
{
    private static readonly WorkflowEventTypes[] ValidOperations = [
        WorkflowEventTypes.Start,
        WorkflowEventTypes.StepEnd,
        WorkflowEventTypes.StepError,
        WorkflowEventTypes.StepTimeout,
        WorkflowEventTypes.DelayEnd
    ];
    protected TWorkflow Workflow = Activator.CreateInstance<TWorkflow>()!;

    protected MessageSerializer MessageSerializer => messageSerializer;

    protected override async ValueTask ProcessMessageAsync(EventMessage message)
    {
        bool isCompleted = false;
        try
        {
            if (!ValidOperations.Any(m=>Equals(m,message.WorkflowEventType)))
                throw new InvalidOperationException($"Unknown event type: {message.WorkflowEventType}");
            await HandleWorkflowEventAsync(await CreateContext(message));
            isCompleted=true;
        }
        catch (WorkflowSuspendedException)
        {
            // handle workflow suspension by doing nothing
        }
        catch (Exception ex)
        {
            Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
            await WorkflowHelper.EndWorkflowAsync(connection, messageSerializer, message, new Messages.WorkflowEnd(DateTime.UtcNow, ex.Message), CancellationToken);
        }
        finally
        {
            await message.Message.AckAsync(cancellationToken: CancellationToken);
        }
        if (isCompleted)
            await WorkflowHelper.EndWorkflowAsync(connection, messageSerializer, message, new Messages.WorkflowEnd(DateTime.UtcNow, null), CancellationToken);
    }

    protected ValueTask<WorkflowContext> CreateContext(EventMessage message)
        => new WorkflowContext(connection, natsJSContext, messageSerializer, timerStore, message)
                .LoadAsync();
    protected abstract ValueTask HandleWorkflowEventAsync(WorkflowContext context);
}
