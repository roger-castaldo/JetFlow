using JetFlow.Data;
using NATS.Client.JetStream;
using System.Text.Json;

namespace JetFlow.Subscriptions;

internal class PerformanceSubscription(INatsJSConsumer consumer, string namespaceName, SubjectMapper subjectMapper, JsonSerializerOptions jsonSerializerOptions, Func<WorkflowPerformanceRecordEvent, ValueTask> workflowRecordRecieved, Func<ActivityPerformanceRecordEvent, ValueTask> activityRecordRecieved, CancellationToken cancellationToken)
    : ASubscription(consumer, cancellationToken)
{
    private readonly string? adjustedNamespaceName = string.IsNullOrWhiteSpace(namespaceName) ? null : namespaceName;
    protected override async ValueTask ProcessMessageAsync(INatsJSMsg<byte[]> msg)
    {
        if (Equals(msg.Subject, subjectMapper.WorkflowPerformanceSubject))
        {
            await workflowRecordRecieved(new(adjustedNamespaceName, JsonSerializer.Deserialize<WorkflowPerformanceRecord>(msg.Data, options: jsonSerializerOptions)));
            await msg.AckAsync();
        }
        else if (Equals(msg.Subject, subjectMapper.ActivityPerformanceSubject))
        {
            await activityRecordRecieved(new(adjustedNamespaceName, JsonSerializer.Deserialize<ActivityPerformanceRecord>(msg.Data, options: jsonSerializerOptions)));
            await msg.AckAsync();
        }
        else
            await msg.NakAsync();
    }

}
