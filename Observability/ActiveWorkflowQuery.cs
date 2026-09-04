using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Serializers;
using NATS.Client.JetStream;
using NATS.Client.ObjectStore;

namespace JetFlow;

internal abstract class WorkflowQueryBase(IJetstreamQuery query,
    INatsObjStore largeMessageStore,
    MessageSerializer messageSerializer,
    SubjectMapper subjectMapper,
    INatsJSContext jsContext,
    Func<Dictionary<string, string[]>?, bool>? checkMetaData) : IWorkflowQuery
{
    private readonly Func<Dictionary<string, string[]>?, bool> CheckMeta = checkMetaData ?? ((meta) => true);
    protected MessageSerializer MessageSerializer => messageSerializer;
    ValueTask IAsyncDisposable.DisposeAsync()
        => query.DisposeAsync();

    private async Task<ActiveWorkflow?> GetActiveWorkflowAsync(INatsJSMsg<byte[]> msg, CancellationToken cancellationToken)
    {
        var eventMessage = await EventMessage.CreateMessageAsync(largeMessageStore, msg, cancellationToken);
        if (CheckMeta(MetaDataHelper.ExtractMetaData(eventMessage.Headers)) && await DoesMessageMatchAsync(eventMessage))
            return await WorkflowHelper.ProduceActiveWorkflowAsync(subjectMapper, jsContext, largeMessageStore, messageSerializer, eventMessage.WorkflowName, eventMessage.WorkflowId, cancellationToken);
        return null;
    }

    async IAsyncEnumerator<ActiveWorkflow> IAsyncEnumerable<ActiveWorkflow>.GetAsyncEnumerator(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await foreach (var msg in query)
            {
                var workflow = await GetActiveWorkflowAsync(msg, cancellationToken);
                if (workflow.HasValue)
                    yield return workflow.Value;
            }
            yield break;
        }
    }

    async ValueTask<IEnumerable<ActiveWorkflow>> IWorkflowQuery.ToListAsync(CancellationToken cancellationToken)
    {
        var tasks = new List<Task<ActiveWorkflow?>>();
        while (!cancellationToken.IsCancellationRequested)
        {
            await foreach (var msg in query)
                tasks.Add(GetActiveWorkflowAsync(msg, cancellationToken));
            break;
        }
        return (await Task.WhenAll(tasks)).Where(w => w.HasValue).Select(w => w!.Value);
    }

    protected virtual ValueTask<bool> DoesMessageMatchAsync(EventMessage eventMessage)
        => ValueTask.FromResult(true);
}

internal class WorkflowQuery(IJetstreamQuery query,
    INatsObjStore largeMessageStore,
    MessageSerializer messageSerializer,
    SubjectMapper subjectMapper, 
    INatsJSContext jsContext,
    Func<Dictionary<string, string[]>?, bool>? checkMetaData) : WorkflowQueryBase(query,
        largeMessageStore,
        messageSerializer,
        subjectMapper,
        jsContext,
        checkMetaData)
{}

internal class WorkflowQuery<TInput>(IJetstreamQuery query,
    INatsObjStore largeMessageStore,
    MessageSerializer messageSerializer,
    SubjectMapper subjectMapper,
    INatsJSContext jsContext,
    Func<Dictionary<string, string[]>?, bool>? checkMetaData,
    Func<TInput, bool>? checkArguement) : WorkflowQueryBase(query,
        largeMessageStore,
        messageSerializer,
        subjectMapper,
        jsContext,
        checkMetaData)
{
    private readonly Func<TInput, bool> CheckArg = checkArguement ?? ((arg) => true);
    protected override async ValueTask<bool> DoesMessageMatchAsync(EventMessage eventMessage)
        => CheckArg(await MessageSerializer.DecodeAsync<TInput>(eventMessage.Data, eventMessage.Headers));
}
