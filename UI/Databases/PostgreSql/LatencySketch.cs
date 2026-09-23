namespace JetFlow.UI;

internal class LatencySketch
{
    private static readonly double multiplier = 1.0/Math.Log(1.01/0.99);

    private Dictionary<int, long> counts = [];
    private long zeroCount = 0;
    public TimeSpan Min { get; private set; } = TimeSpan.MaxValue;
    public TimeSpan Max { get; private set; } = TimeSpan.MinValue;

    public void Add(TimeSpan timeSpan)
    {
        if (Equals(timeSpan, TimeSpan.Zero)) {
            zeroCount++;
            return;
        }
        Min = TimeSpan.FromMicroseconds(Math.Min(Min.TotalMicroseconds, timeSpan.TotalMicroseconds));
        Max = TimeSpan.FromMicroseconds(Math.Max(Max.TotalMicroseconds, timeSpan.TotalMicroseconds));
        var index = (int)Math.Ceiling(Math.Log(timeSpan.TotalMicroseconds) * multiplier);
        if (!counts.TryGetValue(index, out var count))
            counts.Add(index, count);
        else
            counts[index] = count + 1;
    }

    public byte[] AsBinary()
    {
        using var ms = new MemoryStream();
        var bw = new BinaryWriter(ms);
        bw.Write(zeroCount);
        bw.Write(Min.TotalMicroseconds);
        bw.Write(Max.TotalMicroseconds);
        foreach(var pair in counts)
        {
            bw.Write(pair.Key);
            bw.Write(pair.Value);
        }
        bw.Flush();
        return ms.ToArray();
    }

    public static LatencySketch ReadFromByte(byte[] data)
    {
        using var ms = new MemoryStream(data);
        var br = new BinaryReader(ms);
        var zeroCount = br.ReadInt64();
        var min = TimeSpan.FromMicroseconds(br.ReadDouble());
        var max = TimeSpan.FromMicroseconds(br.ReadDouble());
        var counts = new Dictionary<int, long>();
        while (br.BaseStream.Position< br.BaseStream.Length)
        {
            counts.Add(br.ReadInt32(), br.ReadInt64());
        }
        br.Close();
        return new LatencySketch()
        {
            Min=min,
            Max=max,
            counts=counts
        };
    }

    public void Merge(LatencySketch other)
    {
        zeroCount+=other.zeroCount;
        Min = TimeSpan.FromMicroseconds(Math.Min(Min.TotalMicroseconds, other.Min.TotalMicroseconds));
        Max = TimeSpan.FromMicroseconds(Math.Max(Max.TotalMicroseconds, other.Max.TotalMicroseconds));
        foreach(var pair in other.counts)
        {
            if (!counts.TryGetValue(pair.Key, out long value))
                counts.Add(pair.Key, pair.Value);
            else
                counts[pair.Key] = value + pair.Value;
        }
    }

    public TimeSpan GetQuantile(double quantile)
    {
        if (quantile < 0 || quantile > 1)
            throw new ArgumentOutOfRangeException(nameof(quantile));

        if (counts.Count == 0)
            return TimeSpan.Zero;

        long rank = (long)Math.Ceiling(quantile * counts.Values.Sum());

        if (rank < 1)
            rank = 1;

        long cumulative = zeroCount;

        foreach (var bucket in counts.OrderBy(x => x.Key))
        {
            cumulative += bucket.Value;

            if (cumulative >= rank)
                return TimeSpan.FromMicroseconds(Math.Exp(bucket.Key / multiplier));
        }

        return Max;
    }
}
