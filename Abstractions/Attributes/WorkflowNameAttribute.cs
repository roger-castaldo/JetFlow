namespace JetFlow.Attributes;

/// <summary>
/// Specifies a custom name for a workflow class. This attribute can be applied to a workflow class to provide a specific name that can be used for identification or display purposes, instead of relying on the default class name. The provided name can be used in logging, monitoring, or any other context where a human-readable identifier for the workflow is needed.
/// </summary>
/// <param name="name">The custom name for the workflow.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
public class WorkflowNameAttribute(string name) : 
    Attribute
{
    /// <summary>
    /// Gets the custom name specified for the workflow class. This property returns the name provided when the attribute was applied, allowing for easy access to the workflow's identifier in code or during runtime operations.
    /// </summary>
    public string Name { get; } = name;
}
