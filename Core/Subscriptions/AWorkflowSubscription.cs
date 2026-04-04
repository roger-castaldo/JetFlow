using JetFlow.Helpers;
using JetFlow.Messages;
using NATS.Client.Core;
using NATS.Client.JetStream;

namespace JetFlow.Subscriptions;

internal abstract class AWorkflowSubscription<TWorkflow>(NatsConnection connection, INatsJSConsumer consumer, MessageSerializer messageSerializer, CancellationToken cancellationToken)
    where TWorkflow : class
{
    protected NatsConnection Connection => connection;
    protected MessageSerializer MessageSerializer => messageSerializer;

    private Task? runningTask;
    protected TWorkflow Workflow = Activator.CreateInstance<TWorkflow>()!;

    public void Start()
        => runningTask = StartStream();

    private async Task StartStream()
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await consumer.RefreshAsync(cancellationToken); // or try to recreate consumer

                await foreach (var msg in consumer.ConsumeAsync<byte[]>(cancellationToken: cancellationToken))
                {
                    (var workflowName, var workflowId, _, var eventType) = SubjectHelper.ExtractWorkflowEventInfo(msg.Subject);
                    try
                    {
                        await HandleEventAsync(workflowName, workflowId, eventType, msg);
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

    private async ValueTask HandleEventAsync(string workflowName, string workflowId, WorkflowEventTypes eventType, INatsJSMsg<byte[]> msg)
        => await (eventType switch
        {
            WorkflowEventTypes.Start => HandleWorkflowStartAsync(await CreateContext(workflowName, workflowId), msg),
            WorkflowEventTypes.StepEnd => HandleStepEventAsync(await CreateContext(workflowName, workflowId), msg),
            WorkflowEventTypes.StepError => HandleStepEventAsync(await CreateContext(workflowName, workflowId), msg),
            WorkflowEventTypes.StepTimeout => HandleStepEventAsync(await CreateContext(workflowName, workflowId), msg),
            _ => throw new InvalidOperationException($"Unknown event type: {eventType}")
        });

    private async ValueTask<IWorkflowContext> CreateContext(string workflowName, string workflowId)
    {
        var context = new WorkflowContext(workflowName, workflowId);
        // populate context with data from storage if needed
        return await Task.FromResult(context);
    }


    protected abstract ValueTask HandleWorkflowStartAsync(IWorkflowContext context, INatsJSMsg<byte[]> msg);
    protected abstract ValueTask HandleStepEventAsync(IWorkflowContext context, INatsJSMsg<byte[]> msg);
}
