using JetFlow.Helpers;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.KeyValueStore;

namespace JetFlow.Subscriptions;

internal abstract class AWorkflowSubscription<TWorkflow>(INatsConnection connection, INatsJSContext natsJSContext, INatsKVStore timerStore, INatsJSConsumer consumer, 
    MessageSerializer messageSerializer, CancellationToken cancellationToken)
    : ASubscription(connection, natsJSContext, timerStore, messageSerializer, cancellationToken)
    where TWorkflow : class
{
    
    protected TWorkflow Workflow = Activator.CreateInstance<TWorkflow>()!;

    protected override async Task StartStream(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await consumer.RefreshAsync(cancellationToken); // or try to recreate consumer

                await foreach (var msg in consumer.ConsumeAsync<byte[]>(cancellationToken: cancellationToken))
                {
                    (var workflowName, var workflowId, _, var eventType) = SubjectHelper.ExtractWorkflowEventInfo(msg.Subject);
                    bool isCompleted = false;
                    try
                    {
                        if (!Equals(eventType, WorkflowEventTypes.Start) && !Equals(eventType, WorkflowEventTypes.StepEnd)
                            && !Equals(eventType, WorkflowEventTypes.StepError) && !Equals(eventType, WorkflowEventTypes.StepTimeout)
                            && !Equals(eventType, WorkflowEventTypes.DelayEnd))
                            throw new InvalidOperationException($"Unknown event type: {eventType}");
                        await HandleWorkflowEventAsync(await CreateContext(workflowName, workflowId, msg.Metadata?.Sequence));
                        isCompleted=true;
                    }
                    catch (WorkflowSuspendedException)
                    {
                        // handle workflow suspension by doing nothing
                    }
                    catch (Exception ex)
                    {
                        await WorkflowHelper.EndWorkflowAsync<TWorkflow>(Connection, MessageSerializer, workflowId, new Messages.WorkflowEnd(DateTime.UtcNow, ex.Message), cancellationToken);
                    }
                    finally
                    {
                        await msg.AckAsync(cancellationToken: cancellationToken);
                    }
                    if (isCompleted)
                        await WorkflowHelper.EndWorkflowAsync<TWorkflow>(Connection, MessageSerializer, workflowId, new Messages.WorkflowEnd(DateTime.UtcNow, null), cancellationToken);
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

    protected ValueTask<WorkflowContext> CreateContext(string workflowName, string workflowId, NatsJSSequencePair? sequence)
        => new WorkflowContext(Connection, NatsJSContext, MessageSerializer, TimerStore, workflowName, workflowId, sequence)
                .LoadAsync();
    protected abstract ValueTask HandleWorkflowEventAsync(WorkflowContext context);
}
