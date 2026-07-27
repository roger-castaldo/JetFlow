using System;
using System.Collections.Generic;

namespace JetFlow.Testing.Helpers;

internal sealed class SimpleServiceProvider : IServiceProvider
{
    private readonly IDictionary<Type, object?> services;

    public SimpleServiceProvider(IDictionary<Type, object?> services)
    {
        this.services = services ?? new Dictionary<Type, object?>();
    }

    public object? GetService(Type serviceType)
        => services.TryGetValue(serviceType, out var svc) ? svc : null;
}
