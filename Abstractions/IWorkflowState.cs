namespace JetFlow;

public interface IWorkflowState
{
    ValueTask<TValue?> GetActivityResultValueAsync<TWorkflowActivity, TValue>();
    ValueTask<TValue?> GetActivityResultValueAsync<TValue>(string activityName);
}
