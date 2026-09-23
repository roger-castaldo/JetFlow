using JetFlow.Interfaces;
using Shared.Actions;
using Shared.dto;
using System.Security.Cryptography;

namespace InventoryService.Activities;

internal class ReserveInventory : IReserveInventory
{
    Task<bool> IActivityWithReturn<bool, IEnumerable<OrderItem>>.ExecuteAsync(IEnumerable<OrderItem>? input, IWorkflowState state, CancellationToken cancellationToken)
    {
        return Task.FromResult(RandomNumberGenerator.GetInt32(0, 100)<95);
    }
}
