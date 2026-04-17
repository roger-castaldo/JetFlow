namespace JetFlow.Helpers;

internal static class RetryHelper
{
    internal static async Task ProcessActivityRetryAsync(RetryTypes retryType, EventMessage message, ServiceConnection serviceConnection, CancellationToken cancellationToken, Exception? error=null)
    {
        var canRetry = (message.RetryConfiguration?.MaximumAttempts, retryType, message.RetryConfiguration?.RetryOnTimeout, message.RetryConfiguration?.RetryOnError) switch
        {
            (0, RetryTypes.Timeout, true, _) => true,
            (0, RetryTypes.Error, _, true) => true,
            (not null, RetryTypes.Timeout, true, _) => message.ActivityAttempt+1<=message.RetryConfiguration!.MaximumAttempts,
            (not null, RetryTypes.Error, _, true) => message.ActivityAttempt+1<=message.RetryConfiguration!.MaximumAttempts
                && (message.RetryConfiguration.BlockedErrors==null || !message.RetryConfiguration.BlockedErrors.Any(e=>e.Equals(error!.Message, StringComparison.InvariantCultureIgnoreCase))),
            _ => false
        };
        if (canRetry)
            await serviceConnection.RetryActivityAsync(retryType, message, cancellationToken);
        else if (retryType== RetryTypes.Timeout)
            await serviceConnection.TimeoutActivityAsync(message, cancellationToken);
        else if (retryType == RetryTypes.Error)
            await serviceConnection.ErrorActivityAsync(message, error!, cancellationToken);
    }
}
