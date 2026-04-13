using JetFlow.Helpers;
using NATS.Client.JetStream;
using System.Diagnostics;

namespace JetFlow.Subscriptions;

internal abstract class AWorkflowSubscription<TWorkflow>(
    ServiceConnection serviceConnection, SubjectMapper subjectMapper, MessageSerializer messageSerializer,
    INatsJSConsumer consumer, CancellationToken cancellationToken)
    : ASubscription(serviceConnection, consumer, cancellationToken)
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
            MetricsHelper.ProcessWorkflowMessage(message);
            await HandleWorkflowEventAsync(await WorkflowContext.LoadAsync(ServiceConnection, subjectMapper, messageSerializer, message));
            isCompleted=true;
        }
        catch (WorkflowSuspendedException)
        {
            // handle workflow suspension by doing nothing
        }
        catch (Exception ex)
        {
            Activity.Current?.SetStatus(ActivityStatusCode.Error, ex.Message);
            MetricsHelper.EndWorkflow(message.WorkflowName);
            await ServiceConnection.EndWorkflowAsync(message, new(DateTime.UtcNow, ex.Message), CancellationToken);
        }
        finally
        {
            await message.Message.AckAsync(cancellationToken: CancellationToken);
        }
        if (isCompleted)
        {
            MetricsHelper.EndWorkflow(message.WorkflowName);
            await ServiceConnection.EndWorkflowAsync(message, new(DateTime.UtcNow, null), CancellationToken);
        }
    }
    protected abstract ValueTask HandleWorkflowEventAsync(WorkflowContext context);
}
