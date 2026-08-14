using JetFlow.Data;
using NATS.Client.JetStream;
using System.Text.Json;

namespace JetFlow;

internal class PerformanceSubscription
{
    private readonly INatsJSConsumer consumer;
    private readonly SubjectMapper subjectMapper;
    private readonly JsonSerializerOptions jsonSerializerOptions;
    private readonly Func<WorkflowPerformanceRecord, ValueTask> workflowRecordRecieved;
    private readonly Func<ActivityPerformanceRecord, ValueTask> activityRecordRecieved;
    private readonly CancellationToken cancellationToken;
    private readonly Task runningTask;

    public PerformanceSubscription(INatsJSConsumer consumer, SubjectMapper subjectMapper, JsonSerializerOptions jsonSerializerOptions, Func<WorkflowPerformanceRecord, ValueTask> workflowRecordRecieved, Func<ActivityPerformanceRecord, ValueTask> activityRecordRecieved, CancellationToken cancellationToken)
    {
        this.consumer = consumer;
        this.subjectMapper = subjectMapper;
        this.jsonSerializerOptions = jsonSerializerOptions;
        this.workflowRecordRecieved = workflowRecordRecieved;
        this.activityRecordRecieved= activityRecordRecieved;
        this.cancellationToken = cancellationToken;
        runningTask = StartStream();
    }

    private async Task StartStream()
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await consumer.RefreshAsync(cancellationToken); // or try to recreate consumer
                await foreach (var msg in consumer.ConsumeAsync<byte[]>(cancellationToken: cancellationToken))
                {
                    if (Equals(msg.Subject, subjectMapper.WorkflowPerformanceSubject))
                    {
                        await workflowRecordRecieved(JsonSerializer.Deserialize<WorkflowPerformanceRecord>(msg.Data, options: jsonSerializerOptions));
                        await msg.AckAsync();
                    }
                    else if (Equals(msg.Subject, subjectMapper.ActivityPerformanceSubject))
                    {
                        await activityRecordRecieved(JsonSerializer.Deserialize<ActivityPerformanceRecord>(msg.Data, options: jsonSerializerOptions));
                        await msg.AckAsync();
                    }
                    else
                        await msg.NakAsync();
                }
            }
            catch (NatsJSProtocolException)
            {
                //bury error
            }
            catch (NatsJSException)
            {
                // log exception
                await Task.Delay(1000, cancellationToken); // backoff
            }
            catch (OperationCanceledException)
            {
                // expected on cancellation, ignore
            }
        }
    }

    public async Task AwaitClose()
        => await (runningTask.IsCompleted ? Task.CompletedTask : runningTask);
}
