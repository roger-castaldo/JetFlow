namespace JetFlow.Attributes;

/// <summary>
/// Specifies a custom name for an activity class. This attribute can be applied to an activity class to provide a specific name that can be used for identification or display purposes, instead of relying on the default class name. The provided name can be used in logging, monitoring, or any other context where a human-readable identifier for the activity is needed.
/// </summary>
/// <param name="name">The custom name for the activity.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
public class ActivityNameAttribute(string name) :
    Attribute
{
    /// <summary>
    /// Gets the custom name specified for the activity class. This property returns the name provided when the attribute was applied, allowing for easy access to the activity's identifier in code or during runtime operations.
    /// </summary>
    public string Name { get; } = name;
}
