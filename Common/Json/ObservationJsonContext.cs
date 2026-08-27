using System.Text.Json.Serialization;

namespace JetFlow.Data;

[JsonSerializable(typeof(CounterValue))]
[JsonSerializable(typeof(ActivityPerformanceRecord))]
[JsonSerializable(typeof(WorkflowPerformanceRecord))]
internal partial class ObservationJsonContext : JsonSerializerContext;

