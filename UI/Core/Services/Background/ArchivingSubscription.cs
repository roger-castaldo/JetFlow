using JetFlow.Subscriptions;
using JetFlow.UI.Interfaces;
using NATS.Client.JetStream;
using NATS.Client.ObjectStore;
using System.Text.Json;

namespace JetFlow.UI.Services.Background;

internal class ArchivingSubscription
    : ASubscription
{
    public static async ValueTask<ArchivingSubscription> CreateAsync(INatsObjContext objectContext, SubjectMapper subjectMapper, string? namespaceName, 
        IDbConnection dbConnection, INatsJSConsumer consumer, CancellationTokenSource cancellationTokenSource)
    {
        var archiveStore = await objectContext.GetObjectStoreAsync(subjectMapper.WorkflowArchiveObjectstore, cancellationTokenSource.Token);
        return new ArchivingSubscription(archiveStore, namespaceName, dbConnection, consumer, cancellationTokenSource);
    }
    private readonly INatsObjStore archiveStore;
    private readonly string? namespaceName;
    private readonly IDbConnection dbConnection;
    public CancellationTokenSource CancellationTokenSource { get; private init; }
    private ArchivingSubscription(INatsObjStore archiveStore, string? namespaceName, IDbConnection dbConnection, INatsJSConsumer consumer, CancellationTokenSource cancellationTokenSource)
        : base(consumer, cancellationTokenSource.Token)
    {
        this.archiveStore = archiveStore;
        this.namespaceName = namespaceName;
        this.dbConnection = dbConnection;
        CancellationTokenSource = cancellationTokenSource;
    }

    protected override async ValueTask ProcessMessageAsync(INatsJSMsg<byte[]> msg)
    {
        var (workflowName, instance)= EventMessage.ExtractWorkflowFromSubject(msg.Subject);
        var status = await archiveStore.GetInfoAsync($"{workflowName}/{instance}", showDeleted:false, cancellationToken: CancellationToken);
        if (status != null && !status.Deleted)
        {
            var data = await archiveStore.GetBytesAsync($"{workflowName}/{instance}", cancellationToken: CancellationToken);
            var archive = JsonSerializer.Deserialize<ArchivedWorkflow>(data, options: Constants.JsonOptions);
            await dbConnection.StoreArchiveAsync(archive, namespaceName);
            await msg.AckAsync();
            return;
        }
        await msg.NakAsync();
    }
}
