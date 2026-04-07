using NATS.Client.Core;

namespace JetFlow.Helpers;

internal record struct MessageInfo(string Subject, NatsHeaders Headers);
