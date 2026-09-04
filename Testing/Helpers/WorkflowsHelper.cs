using JetFlow.Helpers;
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
                    Name = Guid.CreateVersion7().ToString(), // ephemeral identity
                    DeliverPolicy = ConsumerConfigDeliverPolicy.ByStartTime,
                    AckPolicy = ConsumerConfigAckPolicy.None,
                    FilterSubject = subject,
                    HeadersOnly = false,
                    InactiveThreshold = TimeSpan.FromSeconds(10),
                    OptStartTime = DateTimeOffset.UtcNow
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

    private static async Task<INatsJSMsg<byte[]>?> StartWorkflowAndWait(INatsJSConsumer consumer, Func<ValueTask> close, Func<ValueTask<Guid?>> startCall, Func<Guid?, string, bool> isMatch)
    {
        var completion = new TaskCompletionSource<INatsJSMsg<byte[]>?>();
        Guid? runId = null;
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
                        if (isMatch(runId, msg.Subject))
                        {
                            completion.TrySetResult(msg);
                            exit=true;
                            break;
                        }
                    }
                }
                catch (NatsJSException)
                {
                    await Task.Delay(1000); // backoff
                }catch(Exception ex) when (ex is NatsJSProtocolException || ex is OperationCanceledException)
                {
                    // expected on consumer refresh failure or cancellation, ignore
                }
            }
            await close();
        });
        runId = await startCall();
        return await completion.Task;
    }

    public static async Task<INatsJSMsg<byte[]>?> StartWorkflowAndWaitForCompletion<TWorkflow>(INatsConnection natsConnection, SubjectMapper subjectMapper, Func<ValueTask<Guid?>> startCall)
    {
        var (consumer, close) = await ProduceConsumerAsync(natsConnection, subjectMapper.WorkflowEventsStreamsName, subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<TWorkflow>(), "*"));
        return await StartWorkflowAndWait(consumer, close, startCall, 
            (runId, subject) => (runId.HasValue ?
                Equals(subject, subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<TWorkflow>(), runId.Value.ToString()))
                : IsMatch(subject, subjectMapper.WorkflowEnd(NameHelper.GetWorkflowName<TWorkflow>(), "*"))
             ));
    }

    public static async Task<INatsJSMsg<byte[]>?> StartWorkflowAndWaitForPurge<TWorkflow>(INatsConnection natsConnection, SubjectMapper subjectMapper, Func<ValueTask<Guid?>> startCall)
    {
        var (consumer, close) = await ProduceConsumerAsync(natsConnection, subjectMapper.WorkflowEventsStreamsName, subjectMapper.WorkflowPurged(NameHelper.GetWorkflowName<TWorkflow>(), "*"));
        return await StartWorkflowAndWait(consumer, close, startCall,
            (runId, subject) => (runId.HasValue ?
                Equals(subject, subjectMapper.WorkflowPurged(NameHelper.GetWorkflowName<TWorkflow>(), runId.Value.ToString()))
                : IsMatch(subject, subjectMapper.WorkflowPurged(NameHelper.GetWorkflowName<TWorkflow>(), "*"))
             ));
    }

    public static async Task<INatsJSMsg<byte[]>?> StartWorkflowAndWaitForArchive<TWorkflow>(INatsConnection natsConnection, SubjectMapper subjectMapper, Func<ValueTask<Guid?>> startCall)
    {
        var (consumer, close) = await ProduceConsumerAsync(natsConnection, subjectMapper.WorkflowEventsStreamsName, subjectMapper.WorkflowArchived(NameHelper.GetWorkflowName<TWorkflow>(), "*"));
        return await StartWorkflowAndWait(consumer, close, startCall, (runId, subject) => (runId.HasValue ?
            Equals(subject, subjectMapper.WorkflowArchived(NameHelper.GetWorkflowName<TWorkflow>(), runId.Value.ToString()))
            : IsMatch(subject, subjectMapper.WorkflowArchived(NameHelper.GetWorkflowName<TWorkflow>(), "*"))
         ));
    }

    private static bool IsMatch(string subject, string expectedSubject)
    {
        var subjectParts = subject.Split('.');
        var expectedParts = expectedSubject.Split('.');
        if (subjectParts.Length != expectedParts.Length)
            return false;
        for (int i = 0; i < subjectParts.Length; i++)
        {
            if (expectedParts[i] == "*")
                continue;
            if (subjectParts[i] != expectedParts[i])
                return false;
        }
        return true;
    }
}
