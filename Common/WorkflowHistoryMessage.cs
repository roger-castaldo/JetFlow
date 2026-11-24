using NATS.Client.Core;
using NATS.Client.JetStream;
using System.Collections;

namespace JetFlow;

internal readonly record struct WorkflowHistoryMessage(
    string Subject,
    NatsHeaders? Headers,
    BufferPointer Data);

internal sealed class WorkflowHistory :
    IEnumerable<WorkflowHistoryMessage>,
    IDisposable
{
    private readonly PooledHistoryBuffer buffer = new();
    private readonly List<WorkflowHistoryMessage> messages = [];

    public int Count => messages.Count;

    public void Add(INatsJSMsg<byte[]> message)
    {
        BufferPointer pointer = buffer.Write(message.Data);

        messages.Add(new WorkflowHistoryMessage(
            message.Subject,
            message.Headers,
            pointer));
    }

    public WorkflowHistoryMessage this[int index]
        => messages[index];

    public ReadOnlyMemory<byte>? GetData(
        WorkflowHistoryMessage message)
        => buffer.GetMemory(message.Data);

    public IEnumerator<WorkflowHistoryMessage> GetEnumerator()
        => messages.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public void Dispose()
    {
        messages.Clear();
        buffer.Dispose();
    }
}