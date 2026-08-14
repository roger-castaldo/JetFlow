using System.Numerics;
using System.Text.Json.Serialization;

namespace JetFlow.Data;

public record struct PerformanceCounter(
    string? Namespace,
    BigInteger Value
);
