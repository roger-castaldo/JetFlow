namespace JetFlow;

/// <summary>
/// Details about serviceability for a named worker type, used to report idle/active counts and queue depth.
/// </summary>
/// <param name="IdleInstances">Number of idle instances available.</param>
/// <param name="ActiveInstances">Number of active instances currently processing messages.</param>
/// <param name="MessagesWaiting">Number of messages waiting to be processed.</param>
public record ServiceabilityDetails(int IdleInstances, int ActiveInstances, ulong MessagesWaiting);

/// <summary>
/// Serviceability details including the name of the worker or queue these metrics relate to.
/// </summary>
/// <param name="Name">Name of the service or queue.</param>
/// <param name="IdleInstances">Number of idle instances available.</param>
/// <param name="ActiveInstances">Number of active instances currently processing messages.</param>
/// <param name="MessagesWaiting">Number of messages waiting to be processed.</param>
public record NamedServiceabilityDetails(string Name, int IdleInstances, int ActiveInstances, ulong MessagesWaiting)
    : ServiceabilityDetails(IdleInstances, ActiveInstances, MessagesWaiting);
