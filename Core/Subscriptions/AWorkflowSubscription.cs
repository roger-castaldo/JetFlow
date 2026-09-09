using JetFlow.Configs;
using JetFlow.Helpers;
using JetFlow.Serializers;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client.JetStream;
using System.Diagnostics;

namespace JetFlow.Subscriptions;

internal abstract class AWorkflowSubscription<TWorkflow>(
    ServiceConnection serviceConnection, SubjectMapper subjectMapper, MessageSerializer messageSerializer,
    INatsJSConsumer consumer, MetricsHelper metricsHelper, IServiceProvider? serviceProvider, CancellationToken cancellationToken)
    : AMetricSubscription(serviceConnection, consumer, metricsHelper, cancellationToken)
    where TWorkflow : class
{
    private static readonly WorkflowEventTypes[] ValidOperations = [
        WorkflowEventTypes.Start,
        WorkflowEventTypes.StepEnd,
        WorkflowEventTypes.DelayEnd,
        WorkflowEventTypes.Resumed
    ];
    private static readonly WorkflowEventTypes[] EndOperations = [
        WorkflowEventTypes.End
    ];
    protected TWorkflow Workflow = (serviceProvider!=null ? ActivatorUtilities.CreateInstance<TWorkflow>(serviceProvider) : Activator.CreateInstance<TWorkflow>())!;

    protected MessageSerializer MessageSerializer => messageSerializer;

    protected override async ValueTask ProcessMessageAsync(EventMessage message)
    {
        if (EndOperations.Any(m => Equals(m, message.WorkflowEventType)))
        {
            try
            {
                await ProcessEndOperation(message);
                await message.AckAsync(CancellationToken);
            }
            catch
            {
                await message.NakAsync(CancellationToken);
            }
        }
        else
        {
            bool isCompleted = false;
            try
            {
                if (!ValidOperations.Any(m => Equals(m, message.WorkflowEventType)))
                    throw new InvalidOperationException($"Unknown event type: {message.WorkflowEventType}");
                await MetricsHelper.ProcessWorkflowMessageAsync(message, CancellationToken);
                var context = await WorkflowContext.LoadAsync(ServiceConnection, subjectMapper, messageSerializer, MetricsHelper, message);
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
                await MetricsHelper.EndWorkflowAsync(message.WorkflowName, false, CancellationToken);
                await ServiceConnection.EndWorkflowAsync(message, new(DateTime.UtcNow, ex.Message), CancellationToken);
            }
            await message.AckAsync(CancellationToken);
            if (isCompleted)
            {
                await MetricsHelper.EndWorkflowAsync(message.WorkflowName, true, CancellationToken);
                await ServiceConnection.EndWorkflowAsync(message, new(DateTime.UtcNow, null), CancellationToken);
            }
        }
    }

    private async Task ProcessEndOperation(EventMessage message)
    {
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
        if (Equals(options.CompletionAction, WorkflowCompletionActions.Archive))
        {
            await ServiceConnection.ArchiveStore.PutAsync(
                $"{message.WorkflowName}/{message.WorkflowId}",
                InternalsSerializer.SerializeWorkflowArchive(await WorkflowHelper.ProduceArchivedWorkflowAsync(
                    subjectMapper,
                    ServiceConnection.JSContext,
                    ServiceConnection.LargeMessageStore,
                    MessageSerializer,
                    message.WorkflowName,
                    message.WorkflowId,
                    CancellationToken
                )),
                CancellationToken
            );
            await ServiceConnection.MarkWorkflowArchived(message, CancellationToken);
        }
        await ServiceConnection.MarkWorkflowForPurge(message, options.PurgeDelay, CancellationToken);
    }

    protected abstract ValueTask HandleWorkflowEventAsync(WorkflowContext context);
}
