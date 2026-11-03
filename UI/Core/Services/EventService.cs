using JetFlow.UI.Interfaces;
using JetFlow.UI.Messages;
using JetFlow.UI.Serializers;
using NATS.Client.Core;
using NATS.Client.JetStream;
using System.Collections.Concurrent;
using System.Text.Json;

namespace JetFlow.UI.Services;

internal class EventService : IEventService, IAsyncDisposable
{
    private const string subject = "jetflow.ui.events";

    private readonly CancellationTokenSource cancellationTokenSource = new();
    private readonly EventSerializer serializer = new();
    private readonly ConcurrentDictionary<string, Func<JsonElement?, ValueTask>> callbacks = [];
    private readonly NatsConnection connection;
    private readonly Task runningTask;

    public EventService(NatsOpts natsOpts)
    {
        connection = new NatsConnection(natsOpts);
        runningTask = StartStream();
    }

    private async Task StartStream()
    {
        if (connection.ConnectionState != NatsConnectionState.Open)
        {
            try
            {
                await connection.ConnectAsync();
            }
            catch
            {
                //burying connection errors
            }
        }
        if (connection.ConnectionState != NatsConnectionState.Open)
            throw new UnableToConnectException();
        while (!cancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                await foreach (var msg in connection.SubscribeAsync<Event>(subject, serializer: serializer, cancellationToken: cancellationTokenSource.Token))
                {
                    if (msg.Data!=null && callbacks.TryGetValue(msg.Data.Type, out var callback))
                        await callback(msg.Data.Data);
                }
            }
            catch (NatsJSProtocolException)
            {
                //bury error
            }
            catch (NatsJSException)
            {
                // log exception
                await Task.Delay(1000, cancellationTokenSource.Token); // backoff
            }
            catch (OperationCanceledException)
            {
                // expected on cancellation, ignore
            }
        }
    }

    async ValueTask IEventService.PublishEvent(Event @event)
        => await connection.PublishAsync<Event>(new()
        {
            Subject=subject,
            Data=@event
        }, serializer: serializer, cancellationToken: cancellationTokenSource.Token);

    void IEventService.RegisterEventListener(string type, Func<JsonElement?, ValueTask> callBack)
        => callbacks.TryAdd(type, callBack);

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (!cancellationTokenSource.IsCancellationRequested)
            await cancellationTokenSource.CancelAsync();
        await (runningTask.IsCompleted ? Task.CompletedTask : runningTask);
        await connection.DisposeAsync();
    }
}
