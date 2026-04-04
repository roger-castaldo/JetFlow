using JetFlow;

namespace Sample;

public record User(string FirstName, string LastName);

internal class CreateUserWorkflow : IWorkflow<User>
{
    ValueTask IWorkflow<User>.ExecuteAsync(IWorkflowContext context, User? input)
    {
        Console.WriteLine($"Creating user {input?.FirstName} {input?.LastName}");
        throw new NotImplementedException();
    }
}
