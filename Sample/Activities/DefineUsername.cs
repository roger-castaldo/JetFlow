using JetFlow;

namespace Sample.Activities
{
    internal class DefineUsername : IActivityWithReturn<string, User>
    {
        async ValueTask<string> IActivityWithReturn<string, User>.ExecuteAsync(IWorkflowState state, User? user, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(user, nameof(user));
            return $"{user.FirstName}_{user.LastName}";
        }
    }
}
