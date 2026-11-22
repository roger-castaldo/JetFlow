using NATS.Client.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace JetFlow;

internal readonly record struct WorkflowHistoryMessage(
    string Subject,
    NatsHeaders? Headers,
    BufferPointer Data);

internal sealed class WorkflowHistory :
    IEnumerable<WorkflowHistoryMessage>,
    IDisposable
{
    private readonly PooledHistoryBuffer _buffer = new();
    private readonly List<WorkflowHistoryMessage> _messages = [];

    public int Count => _messages.Count;

    public void Add(
        string subject,
        NatsHeaders? headers,
        ReadOnlySpan<byte> data)
    {
        BufferPointer pointer = _buffer.Write(data);

        _messages.Add(new WorkflowHistoryMessage(
            subject,
            headers,
            pointer));
    }

    public WorkflowHistoryMessage this[int index]
        => _messages[index];

    public ReadOnlyMemory<byte>? GetData(
        WorkflowHistoryMessage message)
        => _buffer.GetMemory(message.Data);

    public IEnumerator<WorkflowHistoryMessage> GetEnumerator()
        => _messages.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public void Dispose()
    {
        _messages.Clear();
        _buffer.Dispose();
    }
}