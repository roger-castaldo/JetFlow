using JetFlow.Data;
using NATS.Client.JetStream;
using System.Text.Json;

namespace JetFlow.Subscriptions;

internal class PerformanceSubscription
    : ASubscription
{
    private readonly SubjectMapper subjectMapper;
    private readonly JsonSerializerOptions jsonSerializerOptions;
    private readonly Func<WorkflowPerformanceRecord, ValueTask> workflowRecordRecieved;
    private readonly Func<ActivityPerformanceRecord, ValueTask> activityRecordRecieved;

    public PerformanceSubscription(INatsJSConsumer consumer, SubjectMapper subjectMapper, JsonSerializerOptions jsonSerializerOptions, Func<WorkflowPerformanceRecord, ValueTask> workflowRecordRecieved, Func<ActivityPerformanceRecord, ValueTask> activityRecordRecieved, CancellationToken cancellationToken)
        : base(consumer, cancellationToken)
    {
        this.subjectMapper = subjectMapper;
        this.jsonSerializerOptions = jsonSerializerOptions;
        this.workflowRecordRecieved = workflowRecordRecieved;
        this.activityRecordRecieved= activityRecordRecieved;
    }

    protected override async ValueTask ProcessMessageAsync(INatsJSMsg<byte[]> msg)
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
