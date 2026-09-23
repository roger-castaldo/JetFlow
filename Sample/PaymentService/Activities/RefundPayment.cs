using JetFlow.Interfaces;
using Shared.Actions;
using Shared.dto;

namespace PaymentService.Activities;

internal class RefundPayment : IRefundPayment
{
    Task IActivity<PaymentRequest>.ExecuteAsync(PaymentRequest? input, IWorkflowState state, CancellationToken cancellationToken)
    {
        Console.WriteLine($"refunding payment for workflow {state.WorkflowID}");
        return Task.CompletedTask;
    }
}
