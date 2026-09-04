namespace JetFlow;

/// <summary>
/// Thrown when an error occurs attempting to connect to the NATS server.  
/// Specifically this will be thrown when the Ping that is executed on each initial connection fails.
/// </summary>
public class ObservationConnectionFailedException : Exception
{
    internal ObservationConnectionFailedException()
        : base("Unable to establish connection to the NATS host") { }
}

/// <summary>
/// Thrown when a requested call is made to a namespace that has not been registered with the observation connection.
/// </summary>
public class NamespaceNotRegisteredException : ArgumentException
{
    internal NamespaceNotRegisteredException(string? namespaceName)
        : base($"The namespace '{namespaceName??"DEFAULT"}' is not registered. Please ensure that the namespace is registered before attempting to use it.") { }
}