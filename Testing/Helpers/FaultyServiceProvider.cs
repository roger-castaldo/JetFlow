namespace JetFlow.Testing.Helpers;

internal sealed class FaultyServiceProvider : IServiceProvider
{
    private readonly Type failType;
    private readonly Exception? exceptionToThrow;

    public FaultyServiceProvider(Type failType, Exception? exceptionToThrow = null)
    {
        this.failType = failType;
        this.exceptionToThrow = exceptionToThrow ?? new InvalidOperationException($"Service {failType.Name} not available");
    }

    public object? GetService(Type serviceType)
    {
        if (serviceType == failType)
            throw exceptionToThrow!;
        return null;
    }
}
