namespace JetFlow;

internal static class Constants
{
    public const string ActivityTimeoutHeader = "x-jetflow-activity-timeout";
    public const string ActivityOverallTimeoutHeader = "x-jetflow-activity-overall-timeout";
    public const string ActivityAttemptHeader = "x-jetflow-activity-attempt";
    public const string ActivityMaximumAttemptsHeader = "x-jetflow-activity-maximum-attempts";
    public const string ActiviyRetryDelayBetweenHeader = "x-jetflow-activity-retry-delay-between";
    public const string ActivityRetryOnTimeoutHeader = "x-jetflow-activity-retry-on-timeout";
    public const string ActivityRetryOnErrorHeader = "x-jetflow-activity-retry-on-error";
    public const string ActivityRetryBlockedErrorsHeader = "x-jetflow-activity-retry-blocked-errors";
}
