using System.Buffers;

namespace JetFlow;

internal readonly record struct BufferPointer(
    int ChunkIndex,
    int Offset,
    int Length);

internal sealed class PooledHistoryBuffer : IDisposable
{
    private const int DefaultChunkSize = 64 * 1024;

    private readonly ArrayPool<byte> pool;
    private readonly List<byte[]> chunks = [];
    private readonly int chunkSize;

    private int currentChunkIndex = -1;
    private int position;
    private bool disposed;

    public PooledHistoryBuffer(
        int chunkSize = DefaultChunkSize,
        ArrayPool<byte>? pool = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chunkSize);

        this.chunkSize = chunkSize;
        this.pool = pool ?? ArrayPool<byte>.Shared;
    }

    public int ChunkCount => chunks.Count;

    public BufferPointer Write(ReadOnlySpan<byte> data)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (data.Length==0)
            return new(0, 0, 0);

        // Large messages get their own pooled array.
        // This avoids splitting a payload across chunks.
        if (data.Length > chunkSize)
        {
            byte[] largeChunk = pool.Rent(data.Length);

            int index = chunks.Count;
            chunks.Add(largeChunk);

            data.CopyTo(largeChunk);

            return new BufferPointer(
                index,
                0,
                data.Length);
        }

        // Allocate another fixed-size chunk when needed.
        if (currentChunkIndex < 0 ||
            chunks[currentChunkIndex].Length - position
                < data.Length)
        {
            byte[] chunk = pool.Rent(chunkSize);

            currentChunkIndex = chunks.Count;
            chunks.Add(chunk);
            position = 0;
        }

        int offset = position;

        data.CopyTo(
            chunks[currentChunkIndex].AsSpan(offset));

        position += data.Length;

        return new BufferPointer(
            currentChunkIndex,
            offset,
            data.Length);
    }

    public ReadOnlyMemory<byte>? GetMemory(BufferPointer pointer)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (pointer.Length==0)
            return null;

        return chunks[pointer.ChunkIndex]
            .AsMemory(pointer.Offset, pointer.Length);
    }

    public void Dispose()
    {
        if (disposed)
            return;

        disposed = true;

        foreach (byte[] chunk in chunks)
        {
            pool.Return(chunk);
        }

        chunks.Clear();
        currentChunkIndex = -1;
        position = 0;
    }
}
