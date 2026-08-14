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