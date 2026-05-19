namespace JetFlow.Interfaces;

/// <summary>
/// Represents a workflow schedule, which can be used to define when a workflow should be executed. The schedule can be defined using a cron expression or any other string representation that the implementation supports.
/// </summary>
public interface IWorkflowSchedule
{
    /// <summary>
    /// The string representation of the workflow schedule. This could be a cron expression or any other format that the implementation supports. The exact format and interpretation of this string will depend on the specific implementation of the workflow scheduling system.
    /// </summary>
    string AsString { get; }
}
