using JetFlow.Configs;
using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Serializers;
using NATS.Client.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JetFlow
{
    internal partial class ServiceConnection
    {
        private async ValueTask<Guid> ScheduleWorkflowAsync<TWorkflow>(string delayString, WorkflowOptions? options, byte[] data, NatsHeaders? headers, CancellationToken cancellationToken)
        {
            var name = NameHelper.GetWorkflowName<TWorkflow>();
            var id = Guid.NewGuid();
            headers ??= new();
            if (options!=null)
                await PublishMessageAsync(new(
                    InternalsSerializer.SerializeWorkflowOptions(options),
                    subjectMapper.ScheduledWorkflowConfigure(name, id.ToString()),
                    new(headers.ToDictionary()),
                    $"{name}-{id}-configure"
                ), cancellationToken: cancellationToken);
            await PublishScheduledMessageAsync(new(
                    data,
                    subjectMapper.ScheduledWorkflowTimer(name, id.ToString()),
                    new(headers.ToDictionary()),
                    $"{name}-{id}-start"
                ), 
                delayString, 
                subjectMapper.ScheduledWorkflowTimer(name, id.ToString()),
                cancellationToken: cancellationToken);
            return id;
        }

        public ValueTask<Guid> ScheduleWorkflowAsync<TWorkflow>(WorkflowSchedule schedule, WorkflowOptions? options, CancellationToken cancellationToken)
        where TWorkflow : IWorkflow
            => ScheduleWorkflowAsync<TWorkflow>(schedule.ToCronString(), options, [], null, cancellationToken);
        public async ValueTask<Guid> ScheduleWorkflowAsync<TWorkflow, TInput>(TInput input, WorkflowSchedule schedule, WorkflowOptions? options, CancellationToken cancellationToken)
            where TWorkflow : IWorkflow<TInput>
        {
            var (data, headers) = await messageSerializer.EncodeAsync<TInput>(input);
            return await ScheduleWorkflowAsync<TWorkflow>(schedule.ToCronString(), options, data, headers, cancellationToken);
        }

        public ValueTask<Guid> DelayStartWorkflowAsync<TWorkflow>(TimeSpan delay, WorkflowOptions? options, CancellationToken cancellationToken)
        where TWorkflow : IWorkflow
            => ScheduleWorkflowAsync<TWorkflow>(CreateScheduledString(delay), options, [], null, cancellationToken);
        public async ValueTask<Guid> DelayStartWorkflowAsync<TWorkflow, TInput>(TInput input, TimeSpan delay, WorkflowOptions? options, CancellationToken cancellationToken)
            where TWorkflow : IWorkflow<TInput>
        {
            var (data, headers) = await messageSerializer.EncodeAsync<TInput>(input);
            return await ScheduleWorkflowAsync<TWorkflow>(CreateScheduledString(delay), options, data, headers, cancellationToken);
        }
    }
}
