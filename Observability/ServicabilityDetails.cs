namespace JetFlow;

public record ServicabilityDetails(int IdleInstances, int ActiveInstances, ulong MessagesWaiting);

public record NamedServicabilityDetails(string Name, int IdleInstances, int ActiveInstances, ulong MessagesWaiting)
    : ServicabilityDetails(IdleInstances, ActiveInstances, MessagesWaiting);