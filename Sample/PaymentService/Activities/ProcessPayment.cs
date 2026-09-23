using JetFlow.Interfaces;
using Shared.Actions;
using Shared.dto;
using System.Security.Cryptography;

namespace PaymentService.Activities;

internal class ProcessPayment : IProcessPayment
{
    Task<bool> IActivityWithReturn<bool, PaymentRequest>.ExecuteAsync(PaymentRequest? input, IWorkflowState state, CancellationToken cancellationToken)
    {
        return Task.FromResult(RandomNumberGenerator.GetInt32(0, 100)<95);
    }
}
