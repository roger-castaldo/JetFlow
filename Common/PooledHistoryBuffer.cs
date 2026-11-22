using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace JetFlow;

internal readonly record struct BufferPointer(
    int ChunkIndex,
    int Offset,
    int Length);

internal sealed class PooledHistoryBuffer : IDisposable
{
    private const int DefaultChunkSize = 64 * 1024;

    private readonly ArrayPool<byte> _pool;
    private readonly List<byte[]> _chunks = [];
    private readonly int _chunkSize;

    private int _currentChunkIndex = -1;
    private int _position;
    private bool _disposed;

    public PooledHistoryBuffer(
        int chunkSize = DefaultChunkSize,
        ArrayPool<byte>? pool = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(chunkSize);

        _chunkSize = chunkSize;
        _pool = pool ?? ArrayPool<byte>.Shared;
    }

    public int ChunkCount => _chunks.Count;

    public BufferPointer Write(ReadOnlySpan<byte> data)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (data.Length==0)
            return new(0, 0, 0);

        // Large messages get their own pooled array.
        // This avoids splitting a payload across chunks.
        if (data.Length > _chunkSize)
        {
            byte[] largeChunk = _pool.Rent(data.Length);

            int index = _chunks.Count;
            _chunks.Add(largeChunk);

            data.CopyTo(largeChunk);

            return new BufferPointer(
                index,
                0,
                data.Length);
        }

        // Allocate another fixed-size chunk when needed.
        if (_currentChunkIndex < 0 ||
            _chunks[_currentChunkIndex].Length - _position
                < data.Length)
        {
            byte[] chunk = _pool.Rent(_chunkSize);

            _currentChunkIndex = _chunks.Count;
            _chunks.Add(chunk);
            _position = 0;
        }

        int offset = _position;

        data.CopyTo(
            _chunks[_currentChunkIndex].AsSpan(offset));

        _position += data.Length;

        return new BufferPointer(
            _currentChunkIndex,
            offset,
            data.Length);
    }

    public ReadOnlyMemory<byte>? GetMemory(BufferPointer pointer)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (pointer.Length==0)
            return null;

        return _chunks[pointer.ChunkIndex]
            .AsMemory(pointer.Offset, pointer.Length);
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        foreach (byte[] chunk in _chunks)
        {
            _pool.Return(chunk);
        }

        _chunks.Clear();
        _currentChunkIndex = -1;
        _position = 0;
    }
}
