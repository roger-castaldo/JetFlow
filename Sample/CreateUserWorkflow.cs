using JetFlow;
using JetFlow.Interfaces;
using Sample.Activities;

namespace Sample;

public record User(string FirstName, string LastName);

internal class CreateUserWorkflow : IWorkflow<User>
{
    async ValueTask IWorkflow<User>.ExecuteAsync(IWorkflowContext context, User? input)
    {
        var usernameResult = await context.ExecuteActivityAsync<DefineUsername, string, User>(new(input));
        if (usernameResult.Status == ActivityResultStatus.Success)
        {
            var uniqueResult = await context.ExecuteActivityAsync<IsUserUnique, bool>(new());
            if (uniqueResult.Status == ActivityResultStatus.Success)
                Console.WriteLine($"Is username unique: {uniqueResult.Output}");
            else
                Console.WriteLine($"Unique username status: {uniqueResult.Status}");
            Console.WriteLine("Starting delay...");
            await context.WaitAsync(TimeSpan.FromSeconds(15));
            Console.WriteLine("Delay complete...");
            var unregisteredResult = await context.ExecuteActivityAsync<UnregisteredActivity>(new()
            {
                Timeouts=new(OverallTimeout: TimeSpan.FromSeconds(5)),
                Retries=new(MaximumAttempts:3)
            });
            Console.WriteLine($"Unregistered result: {unregisteredResult.Status}");
        }else
            Console.WriteLine($"Username status: {usernameResult.Status}");
    }
}
