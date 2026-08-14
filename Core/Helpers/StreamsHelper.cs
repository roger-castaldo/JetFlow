using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;

namespace JetFlow.Helpers;

internal static class StreamsHelper
{
    public static async ValueTask EstablishStreamsAsync(INatsJSContext jsContext, SubjectMapper subjectMapper, Version serverVersion)
    {
        await jsContext.CreateOrUpdateStreamAsync(new(subjectMapper.WorkflowEventsStreamsName, [subjectMapper.WorkflowEventsFilter])
        {
            DuplicateWindow = TimeSpan.FromMinutes(10),
            AllowDirect = true,
            AllowMsgSchedules = true,
            AllowMsgTTL=true,
            AllowAtomicPublish = serverVersion>=new Version("2.12")
        });
        await jsContext.CreateOrUpdateStreamAsync(new(subjectMapper.WorkflowPurgeEventsStreamName, [subjectMapper.WorkflowPurgeEventsFilter])
        {
            DuplicateWindow = TimeSpan.FromMinutes(10),
            AllowMsgSchedules = true,
            Retention = StreamConfigRetention.Workqueue
        });
        await jsContext.CreateOrUpdateStreamAsync(new(subjectMapper.ActivityQueueStream, [subjectMapper.ActivityEventsFilter])
        {
            DuplicateWindow = TimeSpan.FromMinutes(10),
            AllowDirect = true,
            AllowMsgSchedules = true,
            AllowMsgTTL=true,
            Retention = StreamConfigRetention.Workqueue,
            AllowAtomicPublish = serverVersion>=new Version("2.12")
        });
        await jsContext.CreateOrUpdateStreamAsync(new(subjectMapper.ScheduledWorkflowStreamsName, [subjectMapper.ScheduledWorkflowsFilter])
        {
            DuplicateWindow = TimeSpan.FromMinutes(10),
            AllowDirect = true,
            AllowMsgSchedules = true,
            AllowMsgTTL=true,
            AllowAtomicPublish = serverVersion>=new Version("2.12")
        });
        await jsContext.CreateOrUpdateStreamAsync(new(subjectMapper.CountersStreamName, [subjectMapper.CountersFilter])
        {
            AllowMsgCounter = true,
            AllowDirect = true
        });
    }

    public static async ValueTask<(
        INatsJSConsumer activityTimeoutsConsumer,
        INatsJSConsumer scheduledWorkflowConsumer,
        INatsJSConsumer purgeWorkflowConsumer
    )> EstablishBaseConsumersAsync(INatsJSContext jsContext, SubjectMapper subjectMapper)
    {
        var activityTimeoutsConsumer = await jsContext.CreateOrUpdateConsumerAsync(
                    subjectMapper.ActivityQueueStream,
                    new($"jetflow_activity_timeouts")
                    {
                        DurableName = $"jetflow_activity_timeouts",
                        FilterSubject= subjectMapper.ActivityTimeout("*", "*", "*", "*"),
                        AckPolicy = NATS.Client.JetStream.Models.ConsumerConfigAckPolicy.Explicit
                    },
                    CancellationToken.None
                );
        var scheduledWorkflowConsumer = await jsContext.CreateOrUpdateConsumerAsync(
                subjectMapper.ScheduledWorkflowStreamsName,
                new($"jetflow_scheduled_workflows")
                {
                    DurableName = $"jetflow_scheduled_workflows",
                    FilterSubject= subjectMapper.ScheduledWorkflowStart("*", "*"),
                    AckPolicy = NATS.Client.JetStream.Models.ConsumerConfigAckPolicy.Explicit
                },
                CancellationToken.None
            );
        var purgeWorkflowConsumer = await jsContext.CreateOrUpdateConsumerAsync(
                subjectMapper.WorkflowPurgeEventsStreamName,
                new($"jetflow_purge_workflows")
                {
                    DurableName = $"jetflow_purge_workflows",
                    FilterSubject= subjectMapper.WorkflowPurge("*", "*"),
                    AckPolicy = NATS.Client.JetStream.Models.ConsumerConfigAckPolicy.Explicit
                },
                CancellationToken.None
            );
        return (activityTimeoutsConsumer, scheduledWorkflowConsumer, purgeWorkflowConsumer);
    }
}
