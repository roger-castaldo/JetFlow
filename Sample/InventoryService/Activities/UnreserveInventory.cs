using JetFlow.Interfaces;
using Shared.Actions;
using Shared.dto;

namespace InventoryService.Activities;

internal class UnreserveInventory : IUnreserveInventory
{
    Task IActivity<IEnumerable<OrderItem>>.ExecuteAsync(IEnumerable<OrderItem>? input, IWorkflowState state, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Unholding invetory for workflow {state.WorkflowID}");
        return Task.CompletedTask;
    }
}
