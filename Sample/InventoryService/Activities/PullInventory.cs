using JetFlow.Interfaces;
using Shared.Actions;
using Shared.dto;

namespace InventoryService.Activities;

internal class PullInventory : IPullInventory
{
    Task IActivity<IEnumerable<OrderItem>>.ExecuteAsync(IEnumerable<OrderItem>? input, IWorkflowState state, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Inventory pulled for {state.WorkflowID}");
        return Task.CompletedTask;
    }
}
