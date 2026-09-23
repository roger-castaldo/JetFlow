using JetFlow.Attributes;
using JetFlow.Interfaces;
using Shared.dto;

namespace Shared.Actions;

[ActivityName("Reserve Inventory")]
public interface IReserveInventory : IActivityWithReturn<bool, IEnumerable<OrderItem>>;
[ActivityName("Pull Inventory")]
public interface IPullInventory : IActivity<IEnumerable<OrderItem>>;
[ActivityName("Unreserve Inventory")]
public interface IUnreserveInventory : IActivity<IEnumerable<OrderItem>>;
