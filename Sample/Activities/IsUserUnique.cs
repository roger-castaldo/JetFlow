using JetFlow.Interfaces;
using System.Security.Cryptography;

namespace Sample.Activities
{
    internal class IsUserUnique : IActivityWithReturn<bool>
    {
        async ValueTask<bool> IActivityWithReturn<bool>.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            var username = await state.GetActivityResultValueAsync<DefineUsername, string>();
            Console.WriteLine($"Username generated is:{username}");
            return RandomNumberGenerator.GetInt32(2) == 1;
        }
    }
}
