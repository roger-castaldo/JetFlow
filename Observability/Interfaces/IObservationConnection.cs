using JetFlow.Data;
using System.Numerics;

namespace JetFlow.Interfaces;

public interface IObservationConnection
{
    ValueTask<IEnumerable<PerformanceCounter>> GetActiveWorkflowCountAsync();
    ValueTask<BigInteger> GetActiveWorkflowCountAsync(string? workflowNamespace);
    ValueTask<IEnumerable<PerformanceCounter>> GetSuspendedWorkflowCountAsync();
    ValueTask<BigInteger> GetSuspendedWorkflowCountAsync(string? workflowNamespace);
    ValueTask<IEnumerable<PerformanceCounter>> GetActiveActivityCountAsync();
    ValueTask<BigInteger> GetActiveActivityCountAsync(string? workflowName);
    ValueTask AddDefaultNamespaceAsync();
    ValueTask RemoveDefaultNamespaceAsync();
    ValueTask AddNamespaceAsync(string workflowNamespace);
    ValueTask RemoveNamespaceAsync(string workflowNamespace);
    ValueTask AddNamespacesAsync(IEnumerable<string> workflowNamespaces);
    ValueTask RemoveNamespacesAsync(IEnumerable<string> workflowNamespaces);
    ValueTask AddPerformanceMonitoringAsync(byte sampleDurationMinutes, Func<WorkflowPerformanceRecord, ValueTask> workflowRecordReceived, Func<ActivityPerformanceRecord, ValueTask> activityRecordReceived);
}
