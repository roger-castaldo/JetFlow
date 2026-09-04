using System.Collections.Concurrent;
using System.Reflection;
using JetFlow.Attributes;
using JetFlow.Interfaces;

namespace JetFlow.Helpers;

internal static class NameHelper
{
    private static readonly ConcurrentDictionary<Type, string> correctedNames = new();

    public static string GetWorkflowName<TWorkflow>()
    {
        if (!correctedNames.TryGetValue(typeof(TWorkflow), out var name))
        {
            var type = typeof(TWorkflow);
            var resolved = GetWorkflowNameFromType(type)
                ?? type.GetInterfaces()
                    .Where(i => IsWorkflowInterface(i))
                    .Select(i => GetWorkflowNameFromType(i))
                    .FirstOrDefault(n => n is not null)
                ?? TraceBaseWorkflowTypeName(type);
            name = resolved is null ? CleanName(type.Name) : CleanName(resolved);
            correctedNames.TryAdd(type, name);
        }
        return name;
    }

    private static string? TraceBaseWorkflowTypeName(Type type)
    {
        var baseType = type.BaseType;
        string? resolved = null;
        while (resolved is null && baseType is not null)
        {
            if (IsWorkflowImplementation(baseType))
            {
                resolved = GetWorkflowNameFromType(baseType);
                if (resolved is not null)
                    break;

                foreach (var iface in baseType.GetInterfaces())
                {
                    if (!IsWorkflowInterface(iface))
                        continue;

                    resolved = GetWorkflowNameFromType(iface);
                    if (resolved is not null)
                        break;
                }
            }

            baseType = baseType.BaseType;
        }
        return resolved;
    }

    private static string? GetWorkflowNameFromType(Type t)
        => t.GetCustomAttribute<WorkflowNameAttribute>(inherit: false)?.Name;

    private static bool IsWorkflowInterface(Type iface)
        => iface == typeof(IWorkflow)
           || (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IWorkflow<>))
           || iface.GetInterfaces().Any(i => i == typeof(IWorkflow) || (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IWorkflow<>)));

    private static bool IsWorkflowImplementation(Type t)
        => typeof(IWorkflow).IsAssignableFrom(t)
           || t.GetInterfaces().Any(i => IsWorkflowInterface(i));

    public static string GetActivityName<TActivity>()
    {
        if (!correctedNames.TryGetValue(typeof(TActivity), out var name))
        {
            var type = typeof(TActivity);
            var resolved = GetActivityNameFromType(type)
                ?? type.GetInterfaces()
                    .Where(i => IsActivityInterface(i))
                    .Select(i => GetActivityNameFromType(i))
                    .FirstOrDefault(n => n is not null)
                ?? TraceBaseActivityTypeName(type);
            name = resolved is null ? CleanName(type.Name) : CleanName(resolved);
            correctedNames.TryAdd(type, name);
        }
        return name;
    }

    private static string? TraceBaseActivityTypeName(Type type)
    {
        var baseType = type.BaseType;
        string? resolved = null;
        while (resolved is null && baseType is not null)
        {
            if (IsActivityImplementation(baseType))
            {
                resolved = GetActivityNameFromType(baseType);
                if (resolved is not null)
                    break;

                foreach (var iface in baseType.GetInterfaces())
                {
                    if (!IsActivityInterface(iface))
                        continue;

                    resolved = GetActivityNameFromType(iface);
                    if (resolved is not null)
                        break;
                }
            }

            baseType = baseType.BaseType;
        }
        return resolved;
    }

    private static string? GetActivityNameFromType(Type type)
        => type.GetCustomAttribute<ActivityNameAttribute>(inherit: false)?.Name;

    private static bool IsActivityInterface(Type iface)
        => iface == typeof(IActivity)
           || (iface.IsGenericType && (
                iface.GetGenericTypeDefinition() == typeof(IActivity<>) 
                || iface.GetGenericTypeDefinition() == typeof(IActivityWithReturn<>)
                || iface.GetGenericTypeDefinition() == typeof(IActivityWithReturn<,>)
              ))
           || iface.GetInterfaces().Any(i => i == typeof(IActivity) || (i.IsGenericType && (
                i.GetGenericTypeDefinition() == typeof(IActivity<>)
                || i.GetGenericTypeDefinition() == typeof(IActivityWithReturn<>)
                || i.GetGenericTypeDefinition() == typeof(IActivityWithReturn<,>)
           )));

    private static bool IsActivityImplementation(Type t)
        => typeof(IActivity).IsAssignableFrom(t)
           || t.GetInterfaces().Any(i => IsActivityInterface(i));

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
