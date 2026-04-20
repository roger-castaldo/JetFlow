using JetFlow.Interfaces;

namespace Sample.Activities
{
    internal class UnregisteredActivity : IActivity
    {
        Task IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
