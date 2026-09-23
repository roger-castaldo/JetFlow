using JetFlow.Attributes;
using JetFlow.Interfaces;
using Shared.dto;

namespace Shared.Workflows;

[WorkflowName("Place Order")]
public interface IPlaceOrder : IWorkflow<Order>;
