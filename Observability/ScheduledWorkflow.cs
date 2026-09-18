namespace JetFlow;

/// <summary>
/// Houses a reference to a scheduled workflow found in the system
/// </summary>
/// <typeparam name="TInput">The type of the argument if there is one</typeparam>
/// <param name="ID">The id of the scheduled workflow</param>
/// <param name="Name">The name of the scheduled workflow</param>
/// <param name="Arguments">The value of the argument used to start the workflow</param>
/// <param name="MetaData">The metadata associated with the scheduled workflow if any</param>
/// <param name="CronString">The cron string representing the repeating schedule, if a scheduled workflow</param>
/// <param name="RunsAt">The timestamp to indicate when the workflow will run, if it was a delayed workflow</param>
public record ScheduledWorkflow<TInput>(string ID, string Name, TInput? Arguments, IReadOnlyDictionary<string, string[]>? MetaData, string? CronString = null, DateTimeOffset? RunsAt = null);
