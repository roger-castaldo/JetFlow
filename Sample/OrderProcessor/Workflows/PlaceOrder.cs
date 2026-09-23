using JetFlow.Interfaces;
using Shared.Actions;
using Shared.dto;
using Shared.Workflows;

namespace OrderProcessor.Workflows;

internal class PlaceOrder : IPlaceOrder
{
    async ValueTask IWorkflow<Order>.ExecuteAsync(IWorkflowContext context, Order input)
    {
        var reserveItems = await context.ExecuteActivityAsync<IReserveInventory, bool, IEnumerable<OrderItem>>(
            new(input.Items)
            {
                Timeouts = new(OverallTimeout: TimeSpan.FromMinutes(5))
            });
        if (!(reserveItems.Status == JetFlow.ActivityResultStatus.Success && reserveItems.Output))
        {
            Console.WriteLine($"Unable to reserve items for workflow {context.WorkflowID}");
            return;
        }
        var paymentResult = await context.ExecuteActivityAsync<IProcessPayment, bool, PaymentRequest>(
            new(input.PaymentInformation)
            {
                Timeouts = new(OverallTimeout: TimeSpan.FromMinutes(5))
            });
        if (!(paymentResult.Status == JetFlow.ActivityResultStatus.Success && paymentResult.Output))
        {
            Console.WriteLine($"Payment failed for workflow {context.WorkflowID}");
            await context.ExecuteActivityAsync<IUnreserveInventory, IEnumerable<OrderItem>>(new(input.Items));
            return;
        }
        var pullInventory = await context.ExecuteActivityAsync<IPullInventory, IEnumerable<OrderItem>>(new(input.Items));
        if (pullInventory.Status!= JetFlow.ActivityResultStatus.Success)
        {
            Console.WriteLine($"Inventory pull failed for workflow {context.WorkflowID}");
            await context.ExecuteActivityAsync<IRefundPayment, PaymentRequest>(new(input.PaymentInformation));
            return;
        }
        await context.ExecuteActivityAsync<IShipOrder, OrderShipment>(new(input));
    }
}
