namespace JetFlow;

public record ServiceabilityDetails(int IdleInstances, int ActiveInstances, ulong MessagesWaiting);

public record NamedServiceabilityDetails(string Name, int IdleInstances, int ActiveInstances, ulong MessagesWaiting)
    : ServiceabilityDetails(IdleInstances, ActiveInstances, MessagesWaiting);