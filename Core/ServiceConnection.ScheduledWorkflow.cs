using JetFlow.Configs;
using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Serializers;
using NATS.Client.Core;

namespace JetFlow
{
    internal partial class ServiceConnection
    {
        private async ValueTask<Guid> ScheduleWorkflowAsync<TWorkflow>(string delayString, WorkflowOptions? options, Guid id, byte[] data, NatsHeaders? headers, CancellationToken cancellationToken)
        {
            var name = NameHelper.GetWorkflowName<TWorkflow>();
            headers ??= new();
            var messages = new List<InternalNatsConnection.PublishMessage>();
            if (options!=null)
                messages.Add(new(
                    InternalsSerializer.SerializeWorkflowOptions(options),
                    subjectMapper.ScheduledWorkflowConfigure(name, id.ToString()),
                    new(headers.ToDictionary()),
                    $"{name}-{id}-configure"
                ));
            messages.Add(new InternalNatsConnection.ScheduledPublishMessage(
                    data,
                    subjectMapper.ScheduledWorkflowTimer(name, id.ToString()),
                    new(headers.ToDictionary()),
                    $"{name}-{id}-start", 
                    delayString, 
                    subjectMapper.ScheduledWorkflowStart(name, id.ToString())));
            await connection.PublishMessagesAsync(messages, cancellationToken);
            return id;
        }

        public ValueTask<Guid> ScheduleWorkflowAsync<TWorkflow>(IWorkflowSchedule schedule, WorkflowOptions? options, CancellationToken cancellationToken)
        where TWorkflow : IWorkflow
            => ScheduleWorkflowAsync<TWorkflow>(schedule.AsString, options, Guid.NewGuid(), [], null, cancellationToken);
        public async ValueTask<Guid> ScheduleWorkflowAsync<TWorkflow, TInput>(TInput input, IWorkflowSchedule schedule, WorkflowOptions? options, CancellationToken cancellationToken)
            where TWorkflow : IWorkflow<TInput>
        {
            var id = Guid.NewGuid();
            var (data, headers) = await EncodeMessageAsync<TInput>(input, NameHelper.GetWorkflowName<TWorkflow>(), id.ToString(), cancellationToken);
            return await ScheduleWorkflowAsync<TWorkflow>(schedule.AsString, options, id, data, headers, cancellationToken);
        }

        public ValueTask<Guid> DelayStartWorkflowAsync<TWorkflow>(TimeSpan delay, WorkflowOptions? options, CancellationToken cancellationToken)
        where TWorkflow : IWorkflow
            => ScheduleWorkflowAsync<TWorkflow>(InternalNatsConnection.CreateScheduledString(delay), options, Guid.NewGuid(), [], null, cancellationToken);
        public async ValueTask<Guid> DelayStartWorkflowAsync<TWorkflow, TInput>(TInput input, TimeSpan delay, WorkflowOptions? options, CancellationToken cancellationToken)
            where TWorkflow : IWorkflow<TInput>
        {
            var id = Guid.NewGuid();
            var (data, headers) = await EncodeMessageAsync<TInput>(input, NameHelper.GetWorkflowName<TWorkflow>(), id.ToString(), cancellationToken);
            return await ScheduleWorkflowAsync<TWorkflow>(InternalNatsConnection.CreateScheduledString(delay), options, id, data, headers, cancellationToken);
        }
    }
}
