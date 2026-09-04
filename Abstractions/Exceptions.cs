using Microsoft.Extensions.Primitives;

namespace JetFlow;

/// <summary>
/// Thrown when a workflow event message is received with a content type that is not recognized or supported by the system.  This can occur when a message is received with a content type that is not registered in the system, or when a message is received with a content type that is registered but does not have a corresponding decoder or handler.  This can also occur if a message is received with a content type that is registered but is not properly formatted or contains invalid values.
/// </summary>
public class InvalidContentTypeException : NotImplementedException
{
    internal InvalidContentTypeException(StringValues contentType)
        : base($"Content type: {contentType} is unknown, unable to decode") { }
}
