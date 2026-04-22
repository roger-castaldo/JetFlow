using JetFlow.Interfaces;

namespace Sample.Activities
{
    internal class DefineUsername : IActivityWithReturn<string, User>
    {
        async Task<string> IActivityWithReturn<string, User>.ExecuteAsync(User? user, IWorkflowState state, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(user, nameof(user));
            return $"{user.FirstName}_{user.LastName}";
        }
    }
}
