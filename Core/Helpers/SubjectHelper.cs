using System.Text.RegularExpressions;

namespace JetFlow.Helpers;

internal static class SubjectHelper
{
    public const string WorkflowEventsStreamsName = "JETFLOW_WORKFLOW_EVENTS";
    public static string WorkflowStart(string workflowName, string instance)
        => $"wf.{workflowName}.{instance}.start";
    public static string WorkflowEnd(string workflowName, string instance)
        => $"wf.{workflowName}.{instance}.end";
    public static string WorkflowDelayStart(string workflowName, string instance)
        => $"wf.{workflowName}.{instance}.delaystart";
    public static string WorkflowDelayEnd(string workflowName, string instance)
        => $"wf.{workflowName}.{instance}.delayend";
    public static string WorkflowDelayTimer(string workflowName, string instance)
        => $"wf.{workflowName}.{instance}.delaytimer";
    public static string WorkflowStepStart(string workflowName, string instance, string stepName)
        => $"wf.{workflowName}.{instance}.{stepName}.stepstart";
    public static string WorkflowStepEnd(string workflowName, string instance, string stepName)
        => $"wf.{workflowName}.{instance}.{stepName}.stepend";
    public static string WorkflowStepError(string workflowName, string instance, string stepName)
        => $"wf.{workflowName}.{instance}.{stepName}.steperror";
    public static string WorkflowStepTimeout(string workflowName, string instance, string stepName)
        => $"wf.{workflowName}.{instance}.{stepName}.steptimeout";

    private static readonly Regex workflowSubjectRegex = new(@"^wf\.(?<workflowName>[^.]+)\.(?<instance>[^.]+)(?:\.(?<stepName>[^.]+))?\.(?<eventType>start|end|delaystart|delayend|delaytimer|stepstart|stepend|steperror|steptimeout)$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));

    public static (string workflowName,string workflowId,string? stepName,WorkflowEventTypes eventType) ExtractWorkflowEventInfo(string subject)
    {
        var match = workflowSubjectRegex.Match(subject);
        if (!match.Success)
            throw new ArgumentException($"Invalid workflow event subject: {subject}");
        return (
            match.Groups["workflowName"].Value,
            match.Groups["instance"].Value, 
            match.Groups["stepName"].Success ? match.Groups["stepName"].Value : null, 
            Enum.Parse<WorkflowEventTypes>(match.Groups["eventType"].Value, true)
        );
    }

    public const string ActivityEventsStreamsName = "JETFLOW_ACTIVITY_EVENTS";
    public static string ActivityStart(string activityName, string workflowName, string instance)
        => $"act.{activityName}.{workflowName}.{instance}.start";
    public static string ActivityTimer(string activityName, string workflowName, string instance)
        => $"act.{activityName}.{workflowName}.{instance}.timer";
    public static string ActivityTimeout(string activityName, string workflowName, string instance)
        => $"act.{activityName}.{workflowName}.{instance}.timeout";

    private static readonly Regex activitySubjectRegex = new(@"^act\.(?<activityName>[^.]+)\.(?<workflowName>[^.]+)\.(?<instance>[^.]+)\.(?<eventType>start|timer|timeout)$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));

    public static (string activityName, string workflowName, string workflowId, ActivityEventTypes eventType) ExtractActivityEventInfo(string subject)
    {
        var match = activitySubjectRegex.Match(subject);
        if (!match.Success)
            throw new ArgumentException($"Invalid activity event subject: {subject}");
        return (
            match.Groups["activityName"].Value,
            match.Groups["workflowName"].Value,
            match.Groups["instance"].Value,
            Enum.Parse<ActivityEventTypes>(match.Groups["eventType"].Value, true)
        );
    }
}
