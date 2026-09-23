using JetFlow.Attributes;
using JetFlow.Interfaces;
using Shared.dto;

namespace Shared.Actions;

[ActivityName("Process Payment")]
public interface IProcessPayment : IActivityWithReturn<bool, PaymentRequest>;
[ActivityName("Refund Payment")]
public interface IRefundPayment : IActivity<PaymentRequest>;
