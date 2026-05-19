using NATS.Client.Core;
using NATS.Client.JetStream;
using System.Text.Json.Serialization.Metadata;

namespace JetFlow.Configs;

/// <summary>
/// Specifies the compression type to be used for compressing message content when sending messages to NATS. The available options are:
/// </summary>
public enum CompressionTypes
{
    /// <summary>
    /// Brotli is a general-purpose lossless compression algorithm that offers high compression ratios and fast decompression speeds. It is particularly effective for compressing text-based data, such as JSON or XML, making it a good choice for NATS messages that contain structured data.
    /// </summary>
    Brotli,
    /// <summary>
    /// GZip is a widely used compression algorithm that provides a good balance between compression ratio and speed. It is suitable for compressing various types of data, including text and binary formats, making it a versatile option for NATS messages.
    /// </summary>
    GZip
}

/// <summary>
/// Houses the connection options for connecting to NATS, including the NATS connection, JetStream context, JSON type info resolver, compression type, default workflow options, and namespace. This class provides a convenient way to configure and manage the connection settings for interacting with NATS and JetStream.
/// </summary>
public sealed class ConnectionOptions
{
    /// <summary>
    /// Constructs a new instance of the ConnectionOptions class using the provided NatsOpts. This constructor initializes the NATS connection and JetStream context based on the specified options, allowing for easy configuration of the connection settings for interacting with NATS and JetStream.
    /// </summary>
    /// <param name="options">The NatsOpts object containing the configuration options for the NATS connection.</param>
    public ConnectionOptions(NatsOpts options)
        : this(new NatsConnection(options)) { }

    /// <summary>
    /// Constructs a new instance of the ConnectionOptions class using the provided NATS connection. This constructor initializes the JetStream context based on the specified NATS connection, allowing for easy configuration of the connection settings for interacting with NATS and JetStream.
    /// </summary>
    /// <param name="connection">The INatsConnection object representing the NATS connection.</param>
    public ConnectionOptions(INatsConnection connection)
        : this(connection, new NatsJSContext(connection)) { }

    /// <summary>
    /// Constructs a new instance of the ConnectionOptions class using the provided NATS connection and JetStream context. This constructor allows for direct initialization of both the NATS connection and JetStream context, providing flexibility in configuring the connection settings for interacting with NATS and JetStream.
    /// </summary>
    /// <param name="connection"></param>
    /// <param name="jsContext"></param>
    public ConnectionOptions(INatsConnection connection, INatsJSContext jsContext)
    {
        Connection = connection;
        NatsJSContext = jsContext;
    }

    internal INatsConnection Connection { get; private init; }
    internal INatsJSContext NatsJSContext { get; private init; }
    /// <summary>
    /// Uses the System.Text.Json source generator to generate serialization code at compile time, improving performance and reducing memory allocations when serializing and deserializing JSON data. By providing a custom IJsonTypeInfoResolver, you can control how types are serialized and deserialized, allowing for customization of the JSON output and input when working with NATS messages.
    /// </summary>
    public IJsonTypeInfoResolver? JsonTypeInfoResolver { get; init; }
    /// <summary>
    /// Specifies the compression type to be used for compressing message content when sending messages to NATS. The available options are Brotli and GZip, which offer different levels of compression and performance characteristics. By configuring the CompressionType property, you can optimize the size of your NATS messages and improve the efficiency of data transmission, especially when dealing with large payloads or high message volumes.
    /// </summary>
    public CompressionTypes CompressionType { get; init; } = CompressionTypes.Brotli;
    /// <summary>
    /// Specifies the default workflow options to be used when executing workflows in JetFlow. The WorkflowOptions class contains various settings that can be configured to control the behavior of workflows, such as timeouts, retry policies, and error handling strategies. By setting the DefaultWorkflowOptions property, you can ensure that all workflows executed in JetFlow adhere to a consistent set of options, simplifying configuration and improving maintainability.
    /// </summary>
    public WorkflowOptions DefaultWorkflowOptions { get; init; } = new();
    /// <summary>
    /// Used to logically group related workflows and resources within JetFlow. By specifying a namespace, you can organize your workflows and resources in a way that makes it easier to manage and maintain them, especially in larger applications with multiple teams or modules. The Namespace property allows you to create a clear separation between different parts of your application, improving readability and reducing the likelihood of naming conflicts when working with NATS and JetStream.
    /// </summary>
    public string? Namespace { get; init; } = null;
}
