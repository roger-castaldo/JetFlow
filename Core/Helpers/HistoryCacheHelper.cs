using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace JetFlow.Helpers;

internal static class HistoryCacheHelper
{
    internal readonly record struct HistoryCachePolicy(bool ShouldCache, TimeSpan Expiration,CacheItemPriority Priority);

    private static readonly MemoryCache historyCache = new(
        new MemoryCacheOptions
        {
            SizeLimit = 50_000,
            CompactionPercentage = 0.20,
            ExpirationScanFrequency = TimeSpan.FromSeconds(30)
        });

    private static string GetKey(EventMessage message)
        => $"{message.WorkflowName}.{message.WorkflowId}";

    public static WorkflowHistory ObtainHistory(EventMessage message)
    {
        if (historyCache.TryGetValue<WorkflowHistory>(GetKey(message), out var history))
            return history?? [];
        return [];
    }

    public static void CacheHistory(EventMessage message, WorkflowHistory history, WorkflowExitType exitType, TimeSpan? scheduledDelay = null)
    {
        var policy = GetCachePolicy(exitType, history.EstimatedLoadTime, scheduledDelay);
        var key = GetKey(message);
        if (policy?.ShouldCache??false)
            historyCache.Set(key, history, new MemoryCacheEntryOptions()
            {
                SlidingExpiration = policy.Value.Expiration,
                Priority = policy.Value.Priority,
                Size=history.Count
            });
        else
            historyCache.Remove(key);
    }

    public static bool IsCached(EventMessage message)
        => historyCache.TryGetValue(GetKey(message), out _);

    public static void EndWorkflow(EventMessage message)
        => historyCache.Remove(GetKey(message));

    private static HistoryCachePolicy? GetCachePolicy(WorkflowExitType exitType,TimeSpan estimatedLoadDuration,TimeSpan? scheduledDelay = null)
    {
        if (estimatedLoadDuration < TimeSpan.FromMilliseconds(10))
            return null;

        return exitType switch
        {
            WorkflowExitType.Activity =>
                new(true, TimeSpan.FromMinutes(2),
                    CacheItemPriority.High),

            WorkflowExitType.ChildWorkflow =>
                new(true, TimeSpan.FromMinutes(1),
                    CacheItemPriority.Normal),

            WorkflowExitType.Delay
                when scheduledDelay.HasValue &&
                     scheduledDelay.Value <= TimeSpan.FromMinutes(1) =>
                new(true, scheduledDelay.Value + TimeSpan.FromSeconds(30),
                    CacheItemPriority.Normal),

            WorkflowExitType.ExternalEvent =>
                new(true, TimeSpan.FromSeconds(30),
                    CacheItemPriority.Low),

            _ => null
        };
    }
}
