using JetFlow.Interfaces;
using System.Security.Cryptography;

namespace Sample.Activities
{
    internal class IsUserUnique : IActivityWithReturn<bool>
    {
        async Task<bool> IActivityWithReturn<bool>.ExecuteAsync(IWorkflowState state, CancellationToken cancellationToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
            var username = await state.GetActivityResultValueAsync<DefineUsername, string>();
            Console.WriteLine($"Username generated is:{username}");
            return RandomNumberGenerator.GetInt32(2) == 1;
        }
    }
}
