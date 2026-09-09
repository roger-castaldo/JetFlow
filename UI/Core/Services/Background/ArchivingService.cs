using JetFlow.UI.Interfaces;
using Microsoft.Extensions.Hosting;
using NATS.Client.JetStream;
using NATS.Client.JetStream.Models;
using NATS.Client.ObjectStore;
using NATS.Net;
using System.Collections.Concurrent;

namespace JetFlow.UI.Services.Background;

internal class ArchivingService(
        IDbConnection dbConnection,
        INatsJSContext jsContext,
        IConfigService configService
    )
    : BackgroundService
{
    private const string ArchiveStreamName = "JETFLOW_UI_ARCHIVING";
    private const string ArchiveConsumerName = "JETFLOW_UI_ARCHIVING_CONSUMER";
    private readonly ConcurrentDictionary<string, ArchivingSubscription> mappers = [];
    private readonly INatsObjContext natsObjContext = jsContext.CreateObjectStoreContext();
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        stoppingToken.Register(async () =>
        {
            await Task.WhenAll(mappers.Select(pair => Task.Run(async () =>
            {
                await pair.Value.CancellationTokenSource.CancelAsync();
                await pair.Value.AwaitClose();
            })));
        });
        var namespaces = await dbConnection.ListNamespacesAsync();
        await jsContext.CreateOrUpdateStreamAsync(new()
        {
            Name = ArchiveStreamName,
            Subjects = namespaces.Select(ns=>new SubjectMapper(ns).WorkflowArchived("*","*")).ToArray(),
            Retention = StreamConfigRetention.Workqueue
        });
        configService.RegisterAddNamespaceCallback(async ns =>
        {
            if (!mappers.ContainsKey(ns??string.Empty))
            {
                var stream = await jsContext.GetStreamAsync(ArchiveStreamName);
                await jsContext.CreateOrUpdateStreamAsync(new()
                {
                    Name = ArchiveStreamName,
                    Subjects = stream.Info.Config.Subjects!.Append(new SubjectMapper(ns).WorkflowArchived("*","*")).ToArray(),
                    Retention = StreamConfigRetention.Workqueue
                });
                mappers.TryAdd(ns??string.Empty, await CreateArchivingServiceAsync(ns, new CancellationTokenSource()));
            }
        });
        configService.RegisterRemoveNamespaceCallback(async ns =>
        {
            if (mappers.TryRemove(ns??string.Empty, out var subscription))
            {
                await subscription.CancellationTokenSource.CancelAsync();
                await subscription.AwaitClose();
                var sm = new SubjectMapper(ns);
                var stream = await jsContext.GetStreamAsync(ArchiveStreamName);
                await jsContext.CreateOrUpdateStreamAsync(new()
                {
                    Name = ArchiveStreamName,
                    Subjects = stream.Info.Config.Subjects!.Where(s=>!Equals(s,sm.WorkflowArchived("*","*"))).ToArray(),
                    Retention = StreamConfigRetention.Workqueue
                });
            }
        });
    }

    private async ValueTask<ArchivingSubscription> CreateArchivingServiceAsync(string? namespaceName, CancellationTokenSource cancellationTokenSource)
    {
        var subjectMapper = new SubjectMapper(namespaceName);
        return await ArchivingSubscription.CreateAsync(
            natsObjContext,
            subjectMapper,
            namespaceName,
            dbConnection,
            await jsContext.CreateOrUpdateConsumerAsync(
                ArchiveStreamName,
                new()
                {
                    Name=$"{ArchiveConsumerName}_{namespaceName??string.Empty}",
                    DurableName=$"{ArchiveConsumerName}_{namespaceName??string.Empty}",
                    FilterSubject=subjectMapper.WorkflowArchived("*","*"),
                    AckPolicy = ConsumerConfigAckPolicy.Explicit
                }
            ),
            cancellationTokenSource
        );
    }
}
