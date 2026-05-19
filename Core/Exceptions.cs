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

internal class ActivityTimeoutException : TimeoutException
{
    internal ActivityTimeoutException(string activityName) :
        base($"Activity {activityName} has timed out") { }
}

internal class ActivityFailedException : Exception
{
    internal ActivityFailedException(string activityName, string errorMessage) :
        base($"Activity {activityName} has failed with error: {errorMessage}") { }
}

/// <summary>
/// Thrown when an attempt is made to execute an activity on a workflow that has already completed.  This can occur when a workflow completes while an activity is executing, and the activity attempts to report completion after the workflow has already completed.  It can also occur if an activity is attempted to be executed on a workflow that has already completed, but the activity was not aware of the completion.
/// </summary>
public class WorkflowEndedException : InvalidOperationException
{
    internal WorkflowEndedException() : 
        base("You are unable to execute an activity inside a completed workflow") { }
}

/// <summary>
/// Thrown when an attempt is made to execute a step that does not match the expected step.  This can occur when a workflow is executing and the next step is expected to be a specific step, but the step that is attempted to be executed does not match the expected step.  This can also occur if a step is attempted to be executed on a workflow that is not currently executing, but the step was not aware of the workflow's state.
/// </summary>
public class InvalidStepException : InvalidOperationException
{
    internal InvalidStepException(string expected, string actual) :
        base($"Expected step name {expected} but got {actual}") { }
}

/// <summary>
/// Thrown when a workflow event message is received that does not match the expected format for a workflow event message.  This can occur when a message is received that is not a workflow event message, or when a message is received that is a workflow event message but does not contain the expected fields or values.  This can also occur if a message is received that is a workflow event message but is not properly formatted, such as missing required fields or containing invalid values.
/// </summary>
public class InvalidWorkflowEventMessage : InvalidCastException
{
    internal InvalidWorkflowEventMessage(string subject, string id) :
        base($"The message with subject {subject} and id {id} is not a valid workflow event message") { }
}

/// <summary>
/// Thrown when a workflow event message is received with a content type that is not recognized or supported by the system.  This can occur when a message is received with a content type that is not registered in the system, or when a message is received with a content type that is registered but does not have a corresponding decoder or handler.  This can also occur if a message is received with a content type that is registered but is not properly formatted or contains invalid values.
/// </summary>
public class InvalidContentTypeException : NotImplementedException
{
    internal InvalidContentTypeException(StringValues contentType)
        : base($"Content type: {contentType} is unknown, unable to decode") { }
}

/// <summary>
/// Thrown when a delay finished event is received but the subject of the message does not match the expected subject for a delay finished event.  This can occur when a message is received with a subject that is not registered in the system, or when a message is received with a subject that is registered but does not correspond to a delay finished event.  This can also occur if a message is received with a subject that is registered but is not properly formatted or contains invalid values.
/// </summary>
public class InvalidDelayStepException : InvalidOperationException
{
    internal InvalidDelayStepException(string subject) :
        base($"Expected delay finished event but recieved {subject}")
    { }
}