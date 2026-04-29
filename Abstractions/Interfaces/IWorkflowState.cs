namespace JetFlow.Interfaces;

public interface IWorkflowState
{
    ushort ActivityAttempt { get; }
    ValueTask<TValue?> GetActivityResultValueAsync<TWorkflowActivity, TValue>();
    ValueTask<TValue?> GetActivityResultValueAsync<TValue>(string activityName);
}
