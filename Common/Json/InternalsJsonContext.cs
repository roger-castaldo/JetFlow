using JetFlow.Configs;
using System.Text.Json.Serialization;

namespace JetFlow.Data;

[JsonSerializable(typeof(WorkflowEnd))]
[JsonSerializable(typeof(WorkflowOptions))]
[JsonSerializable(typeof(ArchivedWorkflow))]
internal partial class InternalsJsonContext : JsonSerializerContext
{}
