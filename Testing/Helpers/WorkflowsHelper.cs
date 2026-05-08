using JetFlow.Helpers;
using JetFlow.Interfaces;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace JetFlow.Testing.Helpers;

internal static class WorkflowsHelper
{
    private static async ValueTask<(INatsJSConsumer consumer, Func<ValueTask> close)> ProduceConsumerAsync(INatsConnection connection,string stream, string subject)
    {
        var jsContext = new NatsJSContext(connection);
        var consumer = await jsContext.CreateOrUpdateConsumerAsync(
                stream,
                new ConsumerConfig
                {
                    Name = Guid.NewGuid().ToString(), // ephemeral identity
                    DeliverPolicy = ConsumerConfigDeliverPolicy.All,
                    AckPolicy = ConsumerConfigAckPolicy.None,
                    FilterSubject = subject,
                    HeadersOnly = false,
                    InactiveThreshold = TimeSpan.FromSeconds(10)
                }
            );
        return (consumer, async () =>
        {
            try
            {
                await jsContext.DeleteConsumerAsync(consumer.Info.StreamName, consumer.Info.Name);
            }
            catch
            {
                //bury error
            }
        }
        );
    }

    private static async Task<INatsJSMsg<byte[]>?> StartWorkflowAndWait(INatsJSConsumer consumer, Func<ValueTask> close, Func<ValueTask<Guid>> startCall, Func<Guid, string> getSubject)
    {
        var completion = new TaskCompletionSource<INatsJSMsg<byte[]>?>();
        var runId = Guid.Empty;
        _ = Task.Run(async () =>
        {
            var exit = false;
            while (!exit)
            {
                try
                {
                    await consumer.RefreshAsync(); // or try to recreate consumer
                    await foreach (var msg in consumer.ConsumeAsync<byte[]>())
                    {
                        if (Equals(msg.Subject, getSubject(runId)))
                        {
                            completion.TrySetResult(msg);
                            exit=true;
                            break;
                        }
                    }
                }
                catch (NatsJSProtocolException)
                {
                    //bury error
                }
                catch (NatsJSException)
                {
                    // log exception
                    await Task.Delay(1000); // backoff
                }
                catch (OperationCanceledException)
                {
                    // expected on cancellation, ignore
                }
            }
            await close();
        });
        runId= await startCall();
        return await completion.Task;
    }

    public static async Task<INatsJSMsg<byte[]>?> StartWorkflowAndWaitForCompletion<TWorkflow>(INatsConnection natsConnection, SubjectMapper subjectMapper, Func<ValueTask<Guid>> startCall)
    {
        var (consumer, close) = await ProduceConsumerAsync(natsConnection, subjectMapper.WorkflowEventsStreamsName, subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<TWorkflow>(), "*"));
        return await StartWorkflowAndWait(consumer, close, startCall, (runId) => subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<TWorkflow>(), runId.ToString()));
    }

    public static async Task<INatsJSMsg<byte[]>?> StartWorkflowAndWaitForPurge<TWorkflow>(INatsConnection natsConnection, SubjectMapper subjectMapper, Func<ValueTask<Guid>> startCall)
    {
        var (consumer, close) = await ProduceConsumerAsync(natsConnection, subjectMapper.WorkflowEventsStreamsName, subjectMapper.WorkflowPurge(NameHelper.GetWorkflowName<TWorkflow>(), "*"));
        return await StartWorkflowAndWait(consumer, close, startCall, (runId) => subjectMapper.WorkflowPurge(NameHelper.GetWorkflowName<TWorkflow>(), runId.ToString()));
    }

    public static async Task<INatsJSMsg<byte[]>?> StartWorkflowAndWaitForArchive<TWorkflow>(INatsConnection natsConnection, SubjectMapper subjectMapper, Func<ValueTask<Guid>> startCall)
    {
        var (consumer, close) = await ProduceConsumerAsync(natsConnection, subjectMapper.WorkflowEventsStreamsName, subjectMapper.WorkflowArchived(NameHelper.GetWorkflowName<TWorkflow>(), "*"));
        return await StartWorkflowAndWait(consumer, close, startCall, (runId) => subjectMapper.WorkflowArchived(NameHelper.GetWorkflowName<TWorkflow>(), runId.ToString()));
    }
}
