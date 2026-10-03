namespace JetFlow.UI;

/// <summary>
/// Percentile metrics (P50/P95/P99) for latency or duration distributions.
/// </summary>
/// <param name="P50">50th percentile (median) duration.</param>
/// <param name="P95">95th percentile duration.</param>
/// <param name="P99">99th percentile duration.</param>
public record PMetrics(TimeSpan P50, TimeSpan P95, TimeSpan P99);
