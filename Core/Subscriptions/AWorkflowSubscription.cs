using JetFlow.Configs;
using JetFlow.Helpers;
using JetFlow.Serializers;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client.JetStream;
using System.Diagnostics;

namespace JetFlow.Subscriptions;

internal abstract class AWorkflowSubscription<TWorkflow>(
    ServiceConnection serviceConnection, SubjectMapper subjectMapper, MessageSerializer messageSerializer,
    INatsJSConsumer consumer, IServiceProvider? serviceProvider, CancellationToken cancellationToken)
    : ASubscription(serviceConnection, consumer, cancellationToken)
    where TWorkflow : class
{
    private static readonly WorkflowEventTypes[] ValidOperations = [
        WorkflowEventTypes.Start,
        WorkflowEventTypes.StepEnd,
        WorkflowEventTypes.DelayEnd
    ];
    private static readonly WorkflowEventTypes[] EndOperations = [
        WorkflowEventTypes.End,
        WorkflowEventTypes.Purge
    ];
    protected TWorkflow Workflow = (serviceProvider!=null ? ActivatorUtilities.CreateInstance<TWorkflow>(serviceProvider) : Activator.CreateInstance<TWorkflow>())!;

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
                var context = await WorkflowContext.LoadAsync(ServiceConnection, subjectMapper, messageSerializer, message);
                var activityResultStatus = message.WorkflowStepResultStatus;
                var errorMessage = (Equals(message.WorkflowStepResultStatus, ActivityResultStatus.Failure) && message.Data != null ? System.Text.Encoding.UTF8.GetString(message.Data) : null);
                string? timeoutMessage = null;
                if (message.ParallelActivityCount.HasValue)
                {
                    (var isComplete, activityResultStatus, errorMessage, timeoutMessage) = context.ExtractParallelActivityStatus();
                    if (!isComplete)
                        throw new WorkflowSuspendedException();
                }
                if (!string.IsNullOrEmpty(message.ActivityName) && activityResultStatus.HasValue)
                {
                    if (activityResultStatus.Value.HasFlag(ActivityResultStatus.Failure) && context.Options.ErrorOnActivityFailure)
                    {
                        errorMessage??=string.Empty;
                        if (!string.IsNullOrWhiteSpace(timeoutMessage))
                            errorMessage = $"{errorMessage}{(!string.IsNullOrWhiteSpace(errorMessage) ? ";" : "")} {timeoutMessage}";
                        throw new ActivityFailedException(message.ActivityName, errorMessage);
                    }
                    if (activityResultStatus.Value.HasFlag(ActivityResultStatus.Timeout) && context.Options.ErrorOnActivityTimeout)
                        throw new ActivityTimeoutException(message.ActivityName, timeoutMessage);
                }
                await HandleWorkflowEventAsync(context);
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
                await message.AckAsync(CancellationToken);
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
            await message.AckAsync(CancellationToken);
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
            await ServiceConnection.ArchiveWorkflowAsync(message, CancellationToken);
            await ServiceConnection.MarkWorkflowArchived(message, CancellationToken);
        }
        if (Equals(options.CompletionAction, WorkflowCompletionActions.ArchiveThenPurge) || Equals(options.CompletionAction, WorkflowCompletionActions.Purge))
            await ServiceConnection.MarkWorkflowForPurge(message, options.PurgeDelay, CancellationToken);
        await message.AckAsync(CancellationToken);
    }

    protected abstract ValueTask HandleWorkflowEventAsync(WorkflowContext context);
}
