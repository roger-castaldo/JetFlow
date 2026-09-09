using JetFlow.UI.Interfaces;
using JetFlow.UI.Json;
using JetFlow.UI.Services;
using JetFlow.UI.Services.Background;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using NATS.Client.Core;
using NATS.Net;

namespace JetFlow.UI.Extensions;

public static class ServiceCollectionExtension
{
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
        var jsContext = connection.CreateJetStreamContext();
        await dbConnection.InitAsync();
        var observationConnection = await ObservationConnection.CreateInstanceAsync(new(connection));
        var configService = new ConfigService(dbConnection);
        var activeFlowService = new ActiveFlowService(dbConnection, configService, observationConnection);
        services.AddSingleton<IConfigService>(configService)
            .AddSingleton<IActiveFlowService>(activeFlowService)
            .AddHostedService<ArchivingService>(services =>
                new(dbConnection, jsContext, configService)
            )
            .Configure<JsonOptions>(options =>
            {
                options.SerializerOptions.Converters.Add(new BigIntegerConverter());
            });
        return services;
    }
}
