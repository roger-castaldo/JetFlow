using System.Collections.Concurrent;

namespace JetFlow.Helpers;

internal static class NameHelper
{
    private static readonly ConcurrentDictionary<Type, string> correctedNames = new();

    public static string GetWorkflowName<TWorkflow>()
    {
        if (!correctedNames.TryGetValue(typeof(TWorkflow), out var name))
        {
            name = CleanName(typeof(TWorkflow).Name);
            correctedNames.TryAdd(typeof(TWorkflow), name);
        }
        return name;
    }

    public static string GetActivityName<TActivity>()
    {
        if (!correctedNames.TryGetValue(typeof(TActivity), out var name))
        {
            name = CleanName(typeof(TActivity).Name);
            correctedNames.TryAdd(typeof(TActivity), name);
        }
        return name;
    }

    private static string CleanName(string value)
        => new([.. value
            .Select(c=> (char.IsLetterOrDigit(c), c) switch {
                (true, _) => c,
                (false, _) when char.IsWhiteSpace(c) => '_',
                (false, '_') => '_',
                _ => ' '
            })
            .Where(c => !char.IsWhiteSpace(c))
        ]);
}
