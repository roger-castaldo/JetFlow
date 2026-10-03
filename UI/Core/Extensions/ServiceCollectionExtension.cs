using JetFlow.UI.Interfaces;
using JetFlow.UI.Json;
using JetFlow.UI.Services;
using JetFlow.UI.Services.Hosted;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NATS.Client.Core;
using NATS.Net;

namespace JetFlow.UI.Extensions;

/// <summary>
/// Extension methods for registering JetFlow UI services into an IServiceCollection.
/// </summary>
public static class ServiceCollectionExtension
{
    /// <summary>
    /// Registers the JetFlow UI services into the provided <see cref="IServiceCollection"/>.
    /// This will initialize the NATS connection, create the JetStream context, initialize the
    /// database connection, wire up observation listeners, and register UI-related services.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="natsOpts">Options used to configure the NATS connection.</param>
    /// <param name="dbConnection">The database connection implementation to use for persistence.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    /// <exception cref="UnableToConnectException">Thrown when a connection to NATS cannot be established.</exception>
    public static async ValueTask<IServiceCollection> RegisterJetflowUIAsync(this IServiceCollection services,
        NatsOpts natsOpts,
        IDbConnection dbConnection)
    {
        var connection = new NatsConnection(natsOpts);
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
        await dbConnection.InitAsync();
        var configService = new ConfigService(dbConnection);
        var activeFlowService = new ActiveFlowService(dbConnection, configService, await ObservationConnection.CreateInstanceAsync(new(connection)));
        await activeFlowService.InitAsync();
        services.AddSingleton<IConfigService>(configService)
            .AddSingleton<IActiveFlowService>(activeFlowService)
            .AddHostedService<ArchivingObserver>((services)=>new ArchivingObserver(services.GetRequiredService<IDbConnection>(), services.GetRequiredService<IConfigService>(), natsOpts))
            .TryAddSingleton<IDbConnection>(dbConnection);
        services.Configure<JsonOptions>(options =>
            {
                options.SerializerOptions.Converters.Add(new BigIntegerConverter());
            });
        return services;
    }
}
