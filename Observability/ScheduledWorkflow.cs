using System;
using System.Collections.Generic;
using System.Text;

namespace JetFlow;

public record ScheduledWorkflow<TInput>(string ID, string Name, TInput? Arguments, IReadOnlyDictionary<string, string[]>? MetaData, string? CronString = null, DateTimeOffset? RunsAt = null);
