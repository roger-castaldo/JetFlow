using System.Numerics;

namespace JetFlow.Data;

public record struct PerformanceCounter(
    string? Namespace,
    BigInteger Value
);
