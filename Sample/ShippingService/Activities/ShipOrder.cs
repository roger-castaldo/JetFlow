using JetFlow.Interfaces;
using Shared.Actions;
using Shared.dto;

namespace ShippingService.Activities;

internal class ShipOrder : IShipOrder
{
    Task IActivity<OrderShipment>.ExecuteAsync(OrderShipment? input, IWorkflowState state, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Shipping order for workflow {state.WorkflowID}");
        return Task.CompletedTask;
    }
}
