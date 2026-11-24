using System.Text.RegularExpressions;

namespace JetFlow;

internal static partial class RegexPatterns
{
    [GeneratedRegex(
        @"^jetflow\.(?<namespace>[^.]+\.)?(wkf|swf)\.(?<workflowName>[^.]+)\.(?<instance>[^.]+)(?:\.(?<stepName>[^.]+))?(?:\.(?<activityId>[0-9]+))?(?:\.(?<parallelIndex>[0-9]+))?\.(?<eventType>start|end|delaystart|delayend|timer|archived|config|stepstart|stepend|stepretry|suspended|resumed)$", 
        RegexOptions.Compiled, 
        500)]
    public static partial Regex WorkflowSubjectRegex { get; }

    [GeneratedRegex(
        @"^jetflow\.(?<namespace>[^.]+\.)?act\.(?<activityName>[^.]+)\.(?<workflowName>[^.]+)\.(?<instance>[^.]+)\.(?<activityInstance>[^.]+)\.(?<eventType>start|timer|timeout)$", 
        RegexOptions.Compiled, 
        500)]
    public static partial Regex ActivitySubjectRegex { get; }

    [GeneratedRegex(
        @"^jetflow\.(?<namespace>[^.]+\.)?purge\.(?<workflowName>[^.]+)\.(?<instance>[^.]+)$", 
        RegexOptions.Compiled, 
        500)]
    public static partial Regex PurgeWorkflowSubjectRegex { get; }

    internal const string BrotliEncoding = "/brotli";
    internal const string GZipEncoding = "/gzip";

    [GeneratedRegex(
        $"^(?<contenttype>(?!(?:{BrotliEncoding}|{GZipEncoding})$).+?)(?<compression>{BrotliEncoding}|{GZipEncoding})?$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase,
        500)]
    public static partial Regex ContentEncoding { get; }
}
