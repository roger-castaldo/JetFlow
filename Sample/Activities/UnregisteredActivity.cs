using JetFlow;

namespace Sample.Activities
{
    internal class UnregisteredActivity : IActivity
    {
        ValueTask IActivity.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
