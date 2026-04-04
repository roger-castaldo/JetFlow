using JetFlow.Helpers;
using JetFlow.Subscriptions;
using NATS.Client.Core;
using NATS.Client.JetStream;
using System.Text.Json.Serialization.Metadata;

namespace JetFlow;

public static class Connection 
{
    public static async ValueTask<IConnection> CreateInstanceAsync(NatsOpts options,
        IJsonTypeInfoResolver? jsonContext=null)
    {
        var result = new ConnectionInstance(options, jsonContext);
        return await result.OpenAsync();
    }
}

internal class ConnectionInstance : IConnection
{
    private readonly NatsConnection connection;
    private readonly NatsJSContext jsContext;
    private readonly MessageSerializer messageSerializer;
    private readonly CancellationTokenSource cancellationTokenSource = new();

    public ConnectionInstance(NatsOpts options, IJsonTypeInfoResolver? jsonContext)
    {
        connection = new(options);
        jsContext = new(connection);
        messageSerializer = new(jsonContext);
    }

    public async ValueTask<IConnection> OpenAsync()
    {
        await connection.ConnectAsync();
        if (connection.ConnectionState != NatsConnectionState.Open)
            throw new UnableToConnectException();
        await jsContext.CreateOrUpdateStreamAsync(new(SubjectHelper.WorkflowEventsStreamsName, [
            SubjectHelper.WorkflowStart("*", "*"),
            SubjectHelper.WorkflowEnd("*", "*"),
            SubjectHelper.WorkflowStepStart("*", "*", "*"),
            SubjectHelper.WorkflowStepEnd("*", "*", "*"),
            SubjectHelper.WorkflowStepError("*", "*", "*"),
            SubjectHelper.WorkflowStepTimeout("*", "*", "*")
        ])
        {
            DuplicateWindow = TimeSpan.FromMinutes(10)
        });
        await jsContext.CreateOrUpdateStreamAsync(new(SubjectHelper.ActivityEventsStreamsName, [
            SubjectHelper.ActivityStart("*", "*"),
            SubjectHelper.ActivityTimer("*", "*")
        ])
        {
            AllowMsgSchedules = true,
            AllowMsgTTL = true,
            DuplicateWindow = TimeSpan.FromMinutes(10)
        });
        return this;
    }

    private async ValueTask<INatsJSConsumer> CreateConsumerAsync<TWorkflow>(CancellationToken cancellationToken)
    => await jsContext.CreateOrUpdateConsumerAsync(
            SubjectHelper.WorkflowEventsStreamsName,
            new($"wfr_{typeof(TWorkflow).Name}")
            {
                FilterSubjects= [
                    SubjectHelper.WorkflowStart(typeof(TWorkflow).Name, "*"),
                    SubjectHelper.WorkflowStepEnd(typeof(TWorkflow).Name, "*", "*"),
                    SubjectHelper.WorkflowStepError(typeof(TWorkflow).Name, "*", "*"),
                    SubjectHelper.WorkflowStepTimeout(typeof(TWorkflow).Name, "*", "*")
                ],
                DeliverPolicy = NATS.Client.JetStream.Models.ConsumerConfigDeliverPolicy.New,
                AckPolicy = NATS.Client.JetStream.Models.ConsumerConfigAckPolicy.Explicit
            },
            cancellationToken
        );

    async ValueTask IConnection.RegisterWorkflowAsync<TWorkflow>(CancellationToken cancellationToken)
    {
        var sub = new WorkflowSubscription<TWorkflow>(connection, await CreateConsumerAsync<TWorkflow>(cancellationToken), messageSerializer, cancellationTokenSource.Token);
        sub.Start();
    }

    async ValueTask IConnection.RegisterWorkflowAsync<TWorkflow, TInput>(CancellationToken cancellationToken)
    {
        var sub = new WorkflowSubscription<TWorkflow, TInput>(connection, await CreateConsumerAsync<TWorkflow>(cancellationToken), messageSerializer, cancellationTokenSource.Token);
        sub.Start();
    }

    async ValueTask IConnection.RegisterWorkflowAsync<TWorkflow, TInput1, TInput2>(CancellationToken cancellationToken)
    {
        var sub = new WorkflowSubscription<TWorkflow, TInput1, TInput2>(connection, await CreateConsumerAsync<TWorkflow>(cancellationToken), messageSerializer, cancellationTokenSource.Token);
        sub.Start();
    }

    async ValueTask IConnection.RegisterWorkflowAsync<TWorkflow, TInput1, TInput2, TInput3>(CancellationToken cancellationToken)
    {
        var sub = new WorkflowSubscription<TWorkflow, TInput1, TInput2, TInput3>(connection, await CreateConsumerAsync<TWorkflow>(cancellationToken), messageSerializer, cancellationTokenSource.Token);
        sub.Start();
    }

    async ValueTask IConnection.RegisterWorkflowAsync<TWorkflow, TInput1, TInput2, TInput3, TInput4>(CancellationToken cancellationToken)
    {
        var sub = new WorkflowSubscription<TWorkflow, TInput1, TInput2, TInput3, TInput4>(connection, await CreateConsumerAsync<TWorkflow>(cancellationToken), messageSerializer, cancellationTokenSource.Token);
        sub.Start();
    }

    ValueTask IConnection.StartWorkflowAsync<TWorkflow>(CancellationToken cancellationToken)
        => WorkflowHelper.StartWorkflowAsync<TWorkflow>(connection, cancellationToken);

    ValueTask IConnection.StartWorkflowAsync<TWorkflow, TInput>(TInput input, CancellationToken cancellationToken)
        => WorkflowHelper.StartWorkflowAsync<TWorkflow, TInput>(connection, messageSerializer, input, cancellationToken);

    ValueTask IConnection.StartWorkflowAsync<TWorkflow, TInput1, TInput2>(TInput1 input1, TInput2 input2, CancellationToken cancellationToken)
        => WorkflowHelper.StartWorkflowAsync<TWorkflow, TInput1, TInput2>(connection, messageSerializer, input1, input2, cancellationToken);

    ValueTask IConnection.StartWorkflowAsync<TWorkflow, TInput1, TInput2, TInput3>(TInput1 input1, TInput2 input2, TInput3 input3, CancellationToken cancellationToken)
        => WorkflowHelper.StartWorkflowAsync<TWorkflow, TInput1, TInput2, TInput3>(connection, messageSerializer, input1, input2, input3, cancellationToken);

    ValueTask IConnection.StartWorkflowAsync<TWorkflow, TInput1, TInput2, TInput3, TInput4>(TInput1 input, TInput2 input2, TInput3 input3, TInput4 input4, CancellationToken cancellationToken)
        => WorkflowHelper.StartWorkflowAsync<TWorkflow, TInput1, TInput2, TInput3, TInput4>(connection, messageSerializer, input, input2, input3, input4, cancellationToken);
}
