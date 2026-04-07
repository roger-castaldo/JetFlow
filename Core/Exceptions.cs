using Microsoft.Extensions.Primitives;

namespace JetFlow;

/// <summary>
/// Thrown when an error occurs attempting to connect to the NATS server.  
/// Specifically this will be thrown when the Ping that is executed on each initial connection fails.
/// </summary>
public class UnableToConnectException : Exception
{
    internal UnableToConnectException()
        : base("Unable to establish connection to the NATS host") { }
}

internal class WorkflowSuspendedException : Exception
{
    public WorkflowSuspendedException() : base() { }
}

public class WorkflowEndedException : InvalidOperationException
{
    internal WorkflowEndedException() : 
        base("You are unable to execute an activity inside a completed workflow") { }
}

public class InvalidStepException : InvalidOperationException
{
    internal InvalidStepException(string expected, string actual) :
        base($"Expected step name {expected} but got {actual}") { }
}

public class InvalidWorkflowEventMessage : InvalidCastException
{
    internal InvalidWorkflowEventMessage(string subject, string id) :
        base($"The message with subject {subject} and id {id} is not a valid workflow event message") { }
}

public class InvalidContentTypeException : NotImplementedException
{
    internal InvalidContentTypeException(StringValues contentType)
        : base($"Content type: {contentType} is unknown, unable to decode") { }
}

public class InvalidDelayStepException : InvalidOperationException
{
    internal InvalidDelayStepException(string subject) :
        base($"Expected delay finished event but recieved {subject}")
    { }
}