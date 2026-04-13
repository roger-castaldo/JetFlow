using JetFlow.Configs;
using JetFlow.Helpers;
using JetFlow.Serializers;
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
    private static readonly WorkflowEventTypes[] EndOperations = [
        WorkflowEventTypes.End,
        WorkflowEventTypes.Purge
    ];
    protected TWorkflow Workflow = Activator.CreateInstance<TWorkflow>()!;

    protected MessageSerializer MessageSerializer => messageSerializer;

    protected override async ValueTask ProcessMessageAsync(EventMessage message)
    {
        if (EndOperations.Any(m => Equals(m, message.WorkflowEventType)))
            await ProcessEndOperation(message);
        else
        {
            bool isCompleted = false;
            try
            {
                if (!ValidOperations.Any(m => Equals(m, message.WorkflowEventType)))
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
    }

    private async Task ProcessEndOperation(EventMessage message)
    {
        if (Equals(message.WorkflowEventType, WorkflowEventTypes.Purge))
        {
            await message.Message.AckAsync(cancellationToken: CancellationToken);
            await ServiceConnection.PurgeWorkflowAsync(message, CancellationToken);
            return;
        }
        INatsJSMsg<byte[]>? config = null;
        await using var configQuery = await ServiceConnection.QueryStreamAsync(subjectMapper.WorkflowEventsStreamsName, false, subjectMapper.WorkflowConfigure(message.WorkflowName, message.WorkflowId));
        await foreach(var msg in configQuery)
        {
            config = msg;
            break;
        }
        if (config is null)
            throw new InvalidOperationException($"Workflow configuration not found for workflow {message.WorkflowName} with id {message.WorkflowId}");
        var options = InternalsSerializer.DeserializeWorkflowOptions(config.Data!)!;
        if (Equals(options.CompletionAction, WorkflowCompletionActions.ArchiveThenNothing) || Equals(options.CompletionAction, WorkflowCompletionActions.ArchiveThenPurge))
        {
            //archive callback here
            await ServiceConnection.MarkWorkflowArchived(message, CancellationToken);
        }
        if (Equals(options.CompletionAction, WorkflowCompletionActions.ArchiveThenPurge) || Equals(options.CompletionAction, WorkflowCompletionActions.Purge))
            await ServiceConnection.MarkWorkflowForPurge(message, options.PurgeDelay, CancellationToken);
        await message.Message.AckAsync(cancellationToken: CancellationToken);
    }

    protected abstract ValueTask HandleWorkflowEventAsync(WorkflowContext context);
}
