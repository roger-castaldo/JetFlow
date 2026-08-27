using JetFlow.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace JetFlow;

internal static class Constants
{
    public const string HeaderBase = "x-jetflow-";
    public const string ContentEncodingHeader = $"{HeaderBase}content-encoding";
    public const string ActivityTimeoutHeader = $"{HeaderBase}activity-timeout";
    public const string ActivityOverallTimeoutHeader = $"{HeaderBase}activity-overall-timeout";
    public const string ActivityAttemptHeader = $"{HeaderBase}activity-attempt";
    public const string ActivityMaximumAttemptsHeader = $"{HeaderBase}activity-maximum-attempts";
    public const string ActiviyRetryDelayBetweenHeader = $"{HeaderBase}activity-retry-delay-between";
    public const string ActivityRetryOnTimeoutHeader = $"{HeaderBase}activity-retry-on-timeout";
    public const string ActivityRetryOnErrorHeader = $"{HeaderBase}activity-retry-on-error";
    public const string ActivityRetryBlockedErrorsHeader = $"{HeaderBase}activity-retry-blocked-errors";
    public const string ActivityIDHeader = $"{HeaderBase}activity-id";
    public const string SchedulerSourceID = $"{HeaderBase}scheduler-id";
    public const string ActivityResultHeader = $"{HeaderBase}activity-result";
    public const string ParalellActivityIndexHeader = $"{HeaderBase}parallel-activity-index";
    public const string ParallelActivityCountHeader = $"{HeaderBase}parallel-activity-count";

    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented=false,
        AllowTrailingCommas=true,
        PropertyNameCaseInsensitive=true,
        ReadCommentHandling=JsonCommentHandling.Skip,
        DefaultIgnoreCondition=JsonIgnoreCondition.WhenWritingNull,
        TypeInfoResolver = JsonTypeInfoResolver.Combine(InternalsJsonContext.Default, ObservationJsonContext.Default, new DefaultJsonTypeInfoResolver())
    };
}
