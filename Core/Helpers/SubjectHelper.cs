using System.Text.RegularExpressions;

namespace JetFlow.Helpers;

internal static class SubjectHelper
{
    public const string WorkflowEventsStreamsName = "WORKFLOW_EVENTS";
    public static string WorkflowStart(string workflowName, string instance)
        => $"wf.{workflowName}.{instance}.start";
    public static string WorkflowEnd(string workflowName, string instance)
        => $"wf.{workflowName}.{instance}.end";
    public static string WorkflowStepStart(string workflowName, string instance, string stepName)
        => $"wf.{workflowName}.{instance}.{stepName}.start";
    public static string WorkflowStepEnd(string workflowName, string instance, string stepName)
        => $"wf.{workflowName}.{instance}.{stepName}.end";
    public static string WorkflowStepError(string workflowName, string instance, string stepName)
        => $"wf.{workflowName}.{instance}.{stepName}.error";
    public static string WorkflowStepTimeout(string workflowName, string instance, string stepName)
        => $"wf.{workflowName}.{instance}.{stepName}.timeout";

    private static readonly Regex workflowSubjectRegex = new(@"^wf\.(?<workflowName>[^.]+)\.(?<instance>[^.]+)(?:\.(?<stepName>[^.]+))?\.(?<eventType>start|end|error|timeout)$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(500));

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

    public const string ActivityEventsStreamsName = "ACTIVITY_EVENTS";
    public static string ActivityStart(string activityName, string instance)
        => $"act.{activityName}.{instance}.start";
    public static string ActivityTimer(string activityName, string instance)
        => $"act.{activityName}.{instance}.timer";
}
