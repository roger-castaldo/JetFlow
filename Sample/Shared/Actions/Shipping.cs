using JetFlow.Attributes;
using JetFlow.Interfaces;
using Shared.dto;

namespace Shared.Actions;

[ActivityName("Ship Order")]
public interface IShipOrder : IActivity<OrderShipment>;
