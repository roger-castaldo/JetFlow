using JetFlow.Configs;
using JetFlow.Data;
using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Subscriptions;
using NATS.Client.Core;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Client.ObjectStore;
using NATS.Net;
using System.Collections.Concurrent;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JetFlow;

/// <summary>
/// Factory methods for creating <see cref="IObservationConnection"/> instances.
/// Use <see cref="CreateInstanceAsync(ObservationConnectionOptions)"/> to obtain
/// a runtime connection for observing workflow and activity performance data.
/// </summary>
public static class ObservationConnection
{
    /// <summary>
    /// Creates an <see cref="IObservationConnection"/> based on the provided options.
    /// This method performs any necessary connection setup and validation asynchronously.
    /// </summary>
    /// <param name="options">Configuration options used to establish the observation connection.</param>
    /// <returns>A task that resolves to an <see cref="IObservationConnection"/> instance.</returns>
    public static ValueTask<IObservationConnection> CreateInstanceAsync(ObservationConnectionOptions options)
        => ConnectionInstance.CreateAsync(options);

    private class ConnectionInstance(INatsConnection connection, INatsJSContext jsContext, string groupName, bool canDisposeConnection) : IObservationConnection, IAsyncDisposable
    {
        private readonly JsonSerializerOptions jsonOptions = new()
        {
            WriteIndented=false,
            AllowTrailingCommas=true,
            PropertyNameCaseInsensitive=true,
            ReadCommentHandling=JsonCommentHandling.Skip,
            DefaultIgnoreCondition=JsonIgnoreCondition.WhenWritingNull,
            TypeInfoResolver = ObservationJsonContext.Default
        };
        public static async ValueTask<IObservationConnection> CreateAsync(ObservationConnectionOptions options)
        {
            var connection = options.Connection;
            if (connection.ConnectionState != NatsConnectionState.Open)
            {
                try
                {
                    await connection.ConnectAsync();
                }
                catch
                {
                    //burying connection errors
                }
            }
            if (connection.ConnectionState != NatsConnectionState.Open)
                throw new ObservationConnectionFailedException();
            return new ConnectionInstance(connection, new NatsJSContext(connection), options.GroupName, options.CanDisposeConnection);
        }

        private readonly ConcurrentDictionary<string,SubjectMapper> namespaces = [];
        private short samplingDurationMinutes = 5;
        private Func<WorkflowPerformanceRecord, ValueTask>? workflowRecordReceived;
        private Func<ActivityPerformanceRecord, ValueTask>? activityRecordReceived;
        private CancellationTokenSource cancellationTokenSource = new();
        private readonly ConcurrentBag<PerformanceSubscription> subscriptions = new();
        private readonly SemaphoreSlim semaphoreSlim = new(1, 1);


        private async ValueTask RefreshNamespacesAsync() {
            await semaphoreSlim.WaitAsync();
            await cancellationTokenSource.CancelAsync();
            await Task.WhenAll(
                subscriptions.Select(s => s.AwaitClose())
            );
            subscriptions.Clear();
            cancellationTokenSource = new();
            if (workflowRecordReceived!=null && activityRecordReceived!=null)
                await Task.WhenAll(namespaces.Select(async pair =>
                {
                    await jsContext.CreateOrUpdateStreamAsync(new(pair.Value.PerformanceStreamName, [pair.Value.PerformanceFilter])
                    {
                        Retention = StreamConfigRetention.Limits,
                        Discard = StreamConfigDiscard.Old,
                        MaxAge = TimeSpan.FromDays(1)
                    });
                    var kc = jsContext.CreateKeyValueStoreContext();
                    var configStore = await kc.GetStoreAsync(pair.Value.WorkflowConfigKeystore);
                    var createResult = await configStore.TryCreateAsync<short>(pair.Value.PerformanceSamplingKey, samplingDurationMinutes, cancellationToken: cancellationTokenSource.Token);
                    if (!createResult.Success)
                    {
                        var getEntryResult = await configStore.TryGetEntryAsync<short>(pair.Value.PerformanceSamplingKey, cancellationToken: cancellationTokenSource.Token);
                        if(getEntryResult.Success && getEntryResult.Value.Value!= samplingDurationMinutes)
                            await configStore.UpdateAsync<short>(pair.Value.PerformanceSamplingKey, samplingDurationMinutes, getEntryResult.Value.Revision, cancellationToken: cancellationTokenSource.Token);
                    }
                    var consumer = await jsContext.CreateConsumerAsync(
                            pair.Value.PerformanceStreamName,
                            new(groupName)
                            {
                                DurableName=groupName,
                                FilterSubjects=new[] { pair.Value.WorkflowPerformanceSubject, pair.Value.ActivityPerformanceSubject },
                                AckPolicy=NATS.Client.JetStream.Models.ConsumerConfigAckPolicy.Explicit
                            }, cancellationTokenSource.Token
                        );
                    subscriptions.Add(new PerformanceSubscription(consumer, pair.Value, jsonOptions, workflowRecordReceived!, activityRecordReceived!, cancellationTokenSource.Token));
                }));
            semaphoreSlim.Release();
        }

        ValueTask IObservationConnection.AddDefaultNamespaceAsync()
            => ((IObservationConnection)this).AddNamespaceAsync(string.Empty);

        ValueTask IObservationConnection.AddNamespaceAsync(string workflowNamespace)
            => ((IObservationConnection)this).AddNamespacesAsync([workflowNamespace]);

        async ValueTask IObservationConnection.AddNamespacesAsync(IEnumerable<string> workflowNamespaces)
        {
            var added = false;
            foreach (var ns in workflowNamespaces)
                added |= namespaces.TryAdd(ns, new(Equals(string.Empty,ns) ? null : ns));
            if (added)
                await RefreshNamespacesAsync();
        }

        ValueTask IObservationConnection.AddPerformanceMonitoringAsync(byte sampleDurationMinutes, Func<WorkflowPerformanceRecord, ValueTask> workflowRecordReceived, Func<ActivityPerformanceRecord, ValueTask> activityRecordReceived)
        {
            if (this.workflowRecordReceived!= null || this.activityRecordReceived != null)
                throw new InvalidOperationException("Performance monitoring has already been added.");
            if (sampleDurationMinutes<1 || sampleDurationMinutes>10)
                throw new ArgumentOutOfRangeException(nameof(sampleDurationMinutes), "The sampling minutes must be between 1 and 10");
            samplingDurationMinutes = sampleDurationMinutes;
            this.workflowRecordReceived = workflowRecordReceived;
            this.activityRecordReceived = activityRecordReceived;
            return RefreshNamespacesAsync();
        }

        private record struct SubjectStreamPair(string? Namespace, string Stream, string Subject);
        private async ValueTask<IEnumerable<PerformanceCounter>> GetPerformanceCountersAsync(IEnumerable<SubjectStreamPair> pairs)
            => await Task.WhenAll(pairs.Select(
                async (p) =>
                {
                    var stream = await jsContext.GetStreamAsync(p.Stream);
                    try
                    {
                        var msg = await stream.GetAsync(new() { LastBySubj = p.Subject });
                        var value = JsonSerializer.Deserialize<CounterValue>(msg.Message.Data.ToArray(), jsonOptions);
                        return new PerformanceCounter(string.IsNullOrWhiteSpace(p.Namespace) ? null : p.Namespace, BigInteger.Parse(value?.Val??"0"));
                    }
                    catch (NatsJSApiException ex) when (ex.Error.Code == 404)
                    {
                        //no counters available yet
                        return new PerformanceCounter(string.IsNullOrWhiteSpace(p.Namespace) ? null : p.Namespace, BigInteger.Zero);
                    }
                }
            ));

        private async ValueTask<BigInteger> GetPerformanceCounterAsync(string? workflowNamespace, string streamName, string subject)
        {
            var results = await GetPerformanceCountersAsync([new SubjectStreamPair(workflowNamespace, streamName, subject)]);
            return results.First().Value;
        }

        ValueTask<IEnumerable<PerformanceCounter>> IObservationConnection.GetActiveActivityCountAsync()
            => GetPerformanceCountersAsync(namespaces.Select(pair => new SubjectStreamPair(pair.Key, pair.Value.CountersStreamName, pair.Value.ActiveActivitiesCounter)));

        async ValueTask<BigInteger> IObservationConnection.GetActiveActivityCountAsync(string? workflowNamespace)
        {
            if (namespaces.TryGetValue(workflowNamespace??string.Empty, out var mapper))
                return await GetPerformanceCounterAsync(workflowNamespace, mapper.CountersStreamName, mapper.ActiveActivitiesCounter);
            return BigInteger.Zero;
        }

        ValueTask<IEnumerable<PerformanceCounter>> IObservationConnection.GetActiveWorkflowCountAsync()
            => GetPerformanceCountersAsync(namespaces.Select(pair => new SubjectStreamPair(pair.Key, pair.Value.CountersStreamName, pair.Value.ActiveWorkflowsCounter)));

        async ValueTask<BigInteger> IObservationConnection.GetActiveWorkflowCountAsync(string? workflowNamespace)
        {
            if (namespaces.TryGetValue(workflowNamespace??string.Empty, out var mapper))
                return await GetPerformanceCounterAsync(workflowNamespace, mapper.CountersStreamName, mapper.ActiveWorkflowsCounter);
            return BigInteger.Zero;
        }

        ValueTask<IEnumerable<PerformanceCounter>> IObservationConnection.GetSuspendedWorkflowCountAsync()
            => GetPerformanceCountersAsync(namespaces.Select(pair => new SubjectStreamPair(pair.Key, pair.Value.CountersStreamName, pair.Value.SuspendedWorkflowsCounter)));

        async ValueTask<BigInteger> IObservationConnection.GetSuspendedWorkflowCountAsync(string? workflowNamespace)
        {
            if (namespaces.TryGetValue(workflowNamespace??string.Empty, out var mapper))
                return await GetPerformanceCounterAsync(workflowNamespace, mapper.CountersStreamName, mapper.SuspendedWorkflowsCounter);
            return BigInteger.Zero;
        }

        ValueTask IObservationConnection.RemoveNamespaceAsync(string workflowNamespace)
            => ((IObservationConnection)this).RemoveNamespacesAsync([workflowNamespace]);

        async ValueTask IObservationConnection.RemoveNamespacesAsync(IEnumerable<string> workflowNamespaces)
        {
            var removed = false;
            foreach (var ns in workflowNamespaces)
                removed |= namespaces.TryRemove(ns, out _);
            if (removed)
                await RefreshNamespacesAsync();
        }

        ValueTask IObservationConnection.RemoveDefaultNamespaceAsync()
            => ((IObservationConnection)this).RemoveNamespaceAsync(string.Empty);

        private async ValueTask<(INatsObjStore largeMessageStore, SubjectMapper subjectMapper, IJetstreamQuery query)> CreateWorkflowQueryAsync<TWorkflow>(string? workflowNamespace,string workflowId = "*")
        {
            if (!namespaces.TryGetValue(workflowNamespace??string.Empty, out var mapper))
                throw new NamespaceNotRegisteredException(workflowNamespace);
            var objContext = jsContext.CreateObjectStoreContext();
            var largeMessageStore = await objContext.GetObjectStoreAsync(mapper.LargeMessageObjectstore);
            var query = await JetStreamHelper.QueryStreamAsync(
                jsContext,
                mapper.WorkflowEventsStreamsName,
                false,
                mapper.WorkflowStart(NameHelper.GetWorkflowName<TWorkflow>(), workflowId)
            );
            return (largeMessageStore, mapper, query);
        }

        async ValueTask<IWorkflowQuery> IObservationConnection.QueryWorkflowAsync<TWorkflow>(string? workflowNamespace, Func<Dictionary<string, string[]>?, bool>? checkMetaData)
        {
            var (largeMessageStore, subjectMapper, query) = await CreateWorkflowQueryAsync<TWorkflow>(workflowNamespace);
            return new WorkflowQuery(
                query,
                largeMessageStore,
                new(CompressionTypes.Brotli,null),
                subjectMapper,
                jsContext,
                checkMetaData
            );
        }

        async ValueTask<IWorkflowQuery> IObservationConnection.QueryWorkflowAsync<TWorkflow, TInput>(string? workflowNamespace, Func<Dictionary<string, string[]>?, bool>? checkMetaData, Func<TInput, bool>? checkArguement)
        {
            var (largeMessageStore, subjectMapper, query) = await CreateWorkflowQueryAsync<TWorkflow>(workflowNamespace);
            return new WorkflowQuery<TInput>(
                query,
                largeMessageStore,
                new(CompressionTypes.Brotli, null),
                subjectMapper,
                jsContext,
                checkMetaData,
                checkArguement
            );
        }

        async ValueTask<IEnumerable<ActiveWorkflow>> IObservationConnection.LoadWorkflowsAsync<TWorkflow>(string? workflowNamespace, Func<Dictionary<string, string[]>?, bool>? checkMetaData)
            => await (await ((IObservationConnection)this).QueryWorkflowAsync<TWorkflow>(workflowNamespace, checkMetaData)).ToListAsync();

        async ValueTask<IEnumerable<ActiveWorkflow>> IObservationConnection.LoadWorkflowsAsync<TWorkflow, TInput>(string? workflowNamespace, Func<Dictionary<string, string[]>?, bool>? checkMetaData, Func<TInput, bool>? checkArguement)
            => await (await ((IObservationConnection)this).QueryWorkflowAsync<TWorkflow, TInput>(workflowNamespace, checkMetaData, checkArguement)).ToListAsync();

        ValueTask<ActiveWorkflow?> IObservationConnection.LoadWorkflowAsync<TWorkflow>(string? workflowNamespace, Guid workflowId)
            => LoadWorkflowAsync<TWorkflow>(workflowNamespace, workflowId.ToString());
        ValueTask<ActiveWorkflow?> IObservationConnection.LoadWorkflowAsync<TWorkflow, TInput>(string? workflowNamespace, Guid workflowId)
            => LoadWorkflowAsync<TWorkflow>(workflowNamespace, workflowId.ToString());

        private async ValueTask<ActiveWorkflow?> LoadWorkflowAsync<TWorkflow>(string? workflowNamespace, string workflowId)
        {
            var (largeMessageStore, subjectMapper, query) = await CreateWorkflowQueryAsync<TWorkflow>(workflowNamespace, workflowId);
            await using var workflowQuery = new WorkflowQuery(
                query,
                largeMessageStore,
                new(CompressionTypes.Brotli, null),
                subjectMapper,
                jsContext,
                null
            );
            await foreach (var workflow in workflowQuery)
                return workflow;
            return null;
        }

        async ValueTask IAsyncDisposable.DisposeAsync()
        {
            if (!cancellationTokenSource.IsCancellationRequested)
            {
                await semaphoreSlim.WaitAsync();
                await cancellationTokenSource.CancelAsync();
                await Task.WhenAll(
                    subscriptions.Select(s => s.AwaitClose())
                );
                namespaces.Clear();
                subscriptions.Clear();
                if (canDisposeConnection)
                    await connection.DisposeAsync();
                semaphoreSlim.Release();
            }
        }
    }
}
