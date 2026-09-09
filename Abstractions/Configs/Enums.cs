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