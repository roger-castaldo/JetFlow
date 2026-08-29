using NATS.Client.Core;

namespace JetFlow.Configs;

/// <summary>
/// Houses the connection options for connecting to NATS, including the NATS connection and JetStream context. This class provides a convenient way to configure and manage the connection settings for interacting with NATS and JetStream.
/// </summary>
public sealed class ObservationConnectionOptions
{
    /// <summary>
    /// Constructs a new instance of the ObservationConnectionOptions class using the provided NatsOpts. This constructor initializes the NATS connection and JetStream context based on the specified options, allowing for easy configuration of the connection settings for interacting with NATS and JetStream.
    /// </summary>
    /// <param name="options">The NatsOpts object containing the configuration options for the NATS connection.</param>
    public ObservationConnectionOptions(NatsOpts options)
        : this(new NatsConnection(options)) { 
        CanDisposeConnection = true;
    }

    /// <summary>
    /// Constructs a new instance of the ObservationConnectionOptions class using the provided NATS connection. This constructor initializes the JetStream context based on the specified NATS connection, allowing for easy configuration of the connection settings for interacting with NATS and JetStream.
    /// </summary>
    /// <param name="connection">The INatsConnection object representing the NATS connection.</param>
    public ObservationConnectionOptions(INatsConnection connection)
    { 
        Connection = connection;
    }

    internal INatsConnection Connection { get; private init; }
    internal bool CanDisposeConnection { get; private init; } = false;
    /// <summary>
    /// Identifies the connection to listen under a group name, this is used if multiple instances of a given service are being used to share load.
    /// </summary>
    public string GroupName { get; init; } = "JETFLOW_OBSERVATION";
}
