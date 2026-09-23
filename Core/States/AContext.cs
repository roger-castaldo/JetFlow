using JetFlow.Helpers;
using JetFlow.Interfaces;
using JetFlow.Serializers;

namespace JetFlow.States;

internal abstract class AContext(EventMessage startMessage, MessageSerializer messageSerializer) : IContext
{
    public EventMessage StartMessage => startMessage;
    protected MessageSerializer MessageSerializer => messageSerializer;

    private readonly IReadOnlyDictionary<string, string[]>? metaData = MetaDataHelper.ExtractMetaData(startMessage.Headers);

    string IContext.WorkflowID 
        => startMessage?.WorkflowId;

    IReadOnlyDictionary<string, string[]>? IContext.MetaData
        => metaData;

    ValueTask<TInput> IContext.GetWorkflowArgumentAsync<TInput>()
    {
        if (startMessage.Data==null)
            throw new InvalidDataException("There is no argument for this workflow");
        return messageSerializer.DecodeAsync<TInput>(StartMessage.Data, StartMessage.Headers);
    }
}
