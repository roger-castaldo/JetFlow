using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Serializers;
using NATS.Client.Core;

namespace JetFlow
{
    internal partial class ServiceConnection
    {
        private async ValueTask<Guid> ScheduleWorkflowAsync<TWorkflow>(string delayString, WorkflowExecutionRequest? executionRequest, Guid id, byte[] data, NatsHeaders? headers, CancellationToken cancellationToken)
        {
            var name = NameHelper.GetWorkflowName<TWorkflow>();
            headers ??= new();
            var messages = new List<InternalNatsConnection.PublishMessage>();
            if (executionRequest?.Options!=null)
                messages.Add(new(
                    InternalsSerializer.SerializeWorkflowOptions(executionRequest.Options),
                    subjectMapper.ScheduledWorkflowConfigure(name, id.ToString()),
                    new(headers.ToDictionary()),
                    $"{name}-{id}-configure"
                ));
            messages.Add(new InternalNatsConnection.ScheduledPublishMessage(
                    data,
                    subjectMapper.ScheduledWorkflowTimer(name, id.ToString()),
                    new(MetaDataHelper.EncodeMetaData(executionRequest?.MetaData, headers).ToDictionary()),
                    $"{name}-{id}-start", 
                    delayString, 
                    subjectMapper.ScheduledWorkflowStart(name, id.ToString())));
            await connection.PublishMessagesAsync(messages, cancellationToken);
            return id;
        }

        public ValueTask<Guid> ScheduleWorkflowAsync<TWorkflow>(IWorkflowSchedule schedule, WorkflowExecutionRequest? executionRequest, CancellationToken cancellationToken)
        where TWorkflow : IWorkflow
            => ScheduleWorkflowAsync<TWorkflow>(schedule.AsString, executionRequest, Guid.NewGuid(), [], null, cancellationToken);
        public async ValueTask<Guid> ScheduleWorkflowAsync<TWorkflow, TInput>(IWorkflowSchedule schedule, WorkflowExecutionRequest<TInput> executionRequest, CancellationToken cancellationToken)
            where TWorkflow : IWorkflow<TInput>
        {
            var id = Guid.NewGuid();
            var (data, headers) = await EncodeMessageAsync<TInput>(executionRequest.Input, NameHelper.GetWorkflowName<TWorkflow>(), id.ToString(), cancellationToken);
            return await ScheduleWorkflowAsync<TWorkflow>(schedule.AsString, executionRequest, id, data, headers, cancellationToken);
        }

        public ValueTask<Guid> DelayStartWorkflowAsync<TWorkflow>(TimeSpan delay, WorkflowExecutionRequest? executionRequest, CancellationToken cancellationToken)
        where TWorkflow : IWorkflow
            => ScheduleWorkflowAsync<TWorkflow>(InternalNatsConnection.CreateScheduledString(delay), executionRequest, Guid.NewGuid(), [], null, cancellationToken);
        public async ValueTask<Guid> DelayStartWorkflowAsync<TWorkflow, TInput>(TimeSpan delay, WorkflowExecutionRequest<TInput> executionRequest, CancellationToken cancellationToken)
            where TWorkflow : IWorkflow<TInput>
        {
            var id = Guid.NewGuid();
            var (data, headers) = await EncodeMessageAsync<TInput>(executionRequest.Input, NameHelper.GetWorkflowName<TWorkflow>(), id.ToString(), cancellationToken);
            return await ScheduleWorkflowAsync<TWorkflow>(InternalNatsConnection.CreateScheduledString(delay), executionRequest, id, data, headers, cancellationToken);
        }
    }
}
