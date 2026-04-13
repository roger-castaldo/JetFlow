using NATS.Client.JetStream;

namespace JetFlow.Interfaces;

internal interface IJetstreamQuery : IAsyncEnumerable<INatsJSMsg<byte[]>>, IAsyncDisposable
{}
